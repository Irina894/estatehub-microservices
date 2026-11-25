using Microsoft.Data.SqlClient;

namespace Deals.Api.IntegrationEvents;

/// <summary>
/// Доступ до локальної копії довідника Properties (таблиця PropertySnapshots - read model Deals) і до таблиці
/// ProcessedEvents. Політика дублювання з Лаб. 01: Deals зберігає копію назви/ціни обʼєкта для власних запитів;
/// джерело істини - Каталог. Ціни й назви в УЖЕ поданих заявках (Applications.PriceSnapshot / PropertyTitleSnapshot)
/// - це знімок на момент подання, тому події їх НЕ чіпають.
/// </summary>
public sealed class PropertySnapshotStore(IConfiguration configuration, ILogger<PropertySnapshotStore> logger)
{
    private const int PrimaryKeyViolation = 2627;     // порушення PRIMARY KEY / UNIQUE
    private const int UniqueIndexViolation = 2601;

    private static readonly SemaphoreSlim SchemaLock = new(1, 1);
    private static volatile bool _schemaReady;

    private string ConnectionString =>
        configuration.GetConnectionString("DealsDb") ?? throw new InvalidOperationException("ConnectionStrings:DealsDb is not configured");

    /// <summary>
    /// Ідемпотентне застосування події. В ОДНІЙ транзакції: (1) INSERT EventId у ProcessedEvents -
    /// якщо такий уже є (PK), подію вже оброблено й вона пропускається; (2) бізнес-зміна копії.
    /// Повертає false, якщо це дублікат.
    /// </summary>
    public async Task<bool> ApplyOnceAsync(Guid eventId, string eventType, SnapshotChange change, CancellationToken ct)
    {
        await EnsureSchemaAsync(ct);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var tx = (SqlTransaction)await connection.BeginTransactionAsync(ct);

        try
        {
            if (!await TryMarkProcessedAsync(connection, tx, eventId, ct))
            {
                logger.LogInformation("Duplicate event {EventType} {EventId} skipped (already in ProcessedEvents)", eventType, eventId);
                await tx.RollbackAsync(ct);
                return false;
            }

            await ApplyChangeAsync(connection, tx, change, ct);
            await tx.CommitAsync(ct);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;                                    // consumer вирішить: повтор (тимчасовий збій) або DLQ
        }
    }

    private static async Task<bool> TryMarkProcessedAsync(SqlConnection c, SqlTransaction tx, Guid eventId, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("INSERT INTO dbo.ProcessedEvents (EventId) VALUES (@EventId)", c, tx);
        cmd.Parameters.AddWithValue("@EventId", eventId);
        try
        {
            await cmd.ExecuteNonQueryAsync(ct);
            return true;
        }
        catch (SqlException ex) when (ex.Number is PrimaryKeyViolation or UniqueIndexViolation)
        {
            return false;                             // «уже оброблено»
        }
    }

    /// <summary>
    /// MERGE з перевіркою часу: застарілу подію (OccurredAt не новіший за LastEventAt), що прийшла пізніше за новішу
    /// (out-of-order), НЕ застосовуємо, щоб не перезаписати свіжіші дані. COALESCE залишає поля, яких подія не несе.
    /// </summary>
    private static async Task ApplyChangeAsync(SqlConnection c, SqlTransaction tx, SnapshotChange ch, CancellationToken ct)
    {
        const string sql = """
            MERGE dbo.PropertySnapshots WITH (HOLDLOCK) AS t
            USING (SELECT @PropertyId AS PropertyId) AS s ON t.PropertyId = s.PropertyId
            WHEN MATCHED AND t.LastEventAt < @At THEN
                UPDATE SET Title       = COALESCE(@Title, t.Title),
                           Price       = COALESCE(@Price, t.Price),
                           City        = COALESCE(@City, t.City),
                           Status      = COALESCE(@Status, t.Status),
                           IsDeleted   = COALESCE(@IsDeleted, t.IsDeleted),
                           LastEventAt = @At
            WHEN NOT MATCHED THEN
                INSERT (PropertyId, Title, Price, City, Status, IsDeleted, LastEventAt)
                VALUES (@PropertyId, @Title, @Price, @City, @Status, COALESCE(@IsDeleted, 0), @At);
            """;

        await using var cmd = new SqlCommand(sql, c, tx);
        cmd.Parameters.AddWithValue("@PropertyId", ch.PropertyId);
        cmd.Parameters.Add(new SqlParameter("@At", System.Data.SqlDbType.DateTime2) { Value = DateTime.SpecifyKind(ch.OccurredAt, DateTimeKind.Utc) });
        cmd.Parameters.AddWithValue("@Title", (object?)ch.Title ?? DBNull.Value);
        cmd.Parameters.Add(new SqlParameter("@Price", System.Data.SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = (object?)ch.Price ?? DBNull.Value });
        cmd.Parameters.AddWithValue("@City", (object?)ch.City ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", (object?)ch.Status ?? DBNull.Value);
        cmd.Parameters.Add(new SqlParameter("@IsDeleted", System.Data.SqlDbType.Bit) { Value = (object?)ch.IsDeleted ?? DBNull.Value });
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<PropertySnapshotRow>> ListAsync(CancellationToken ct)
    {
        await EnsureSchemaAsync(ct);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "SELECT PropertyId, Title, Price, City, Status, IsDeleted, LastEventAt FROM dbo.PropertySnapshots ORDER BY LastEventAt DESC",
            connection);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var rows = new List<PropertySnapshotRow>();
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new PropertySnapshotRow(
                reader.GetGuid(0), reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetDecimal(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetBoolean(5), reader.GetDateTime(6)));
        }

        return rows;
    }

    /// <summary>
    /// Створює таблиці, якщо їх ще немає (ті самі оператори є в db/p1/schema.sql). Запускається один раз на процес,
    /// тож сервіс працює і тоді, коли скрипт схеми ще не виконували повторно після Лаб. 8.
    /// </summary>
    private async Task EnsureSchemaAsync(CancellationToken ct)
    {
        if (_schemaReady) return;

        await SchemaLock.WaitAsync(ct);
        try
        {
            if (_schemaReady) return;

            const string sql = """
                IF OBJECT_ID('dbo.ProcessedEvents', 'U') IS NULL
                CREATE TABLE dbo.ProcessedEvents (
                    EventId     UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    ProcessedAt DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
                );

                IF OBJECT_ID('dbo.PropertySnapshots', 'U') IS NULL
                CREATE TABLE dbo.PropertySnapshots (
                    PropertyId  UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    Title       NVARCHAR(200)    NOT NULL,
                    Price       DECIMAL(18,2)    NULL,
                    City        NVARCHAR(100)    NULL,
                    Status      NVARCHAR(30)     NULL,
                    IsDeleted   BIT              NOT NULL DEFAULT 0,
                    LastEventAt DATETIME2        NOT NULL
                );
                """;

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(ct);
            await using var cmd = new SqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync(ct);

            _schemaReady = true;
            logger.LogInformation("Deals event tables (ProcessedEvents, PropertySnapshots) are ready");
        }
        finally
        {
            SchemaLock.Release();
        }
    }
}

/// <summary>Що саме змінює подія в копії (null = поле не змінюється).</summary>
public sealed record SnapshotChange(
    Guid PropertyId, DateTime OccurredAt, string? Title = null, decimal? Price = null,
    string? City = null, string? Status = null, bool? IsDeleted = null);

public sealed record PropertySnapshotRow(
    Guid PropertyId, string Title, decimal? Price, string? City, string? Status, bool IsDeleted, DateTime LastEventAt);
