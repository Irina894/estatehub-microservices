using System.Data;
using Deals.Domain.Interfaces;
using Deals.Domain.Models;
using Microsoft.Data.SqlClient;

namespace Deals.Dal.Repositories;

/// <summary>Репозиторій на чистому ADO.NET: SqlCommand + SqlDataReader, усі значення - параметри.</summary>
public sealed class ApplicationRepository(DbSession session) : IApplicationRepository
{
    private const string Columns =
        "Id, ClientId, PropertyId, PriceSnapshot, PropertyTitleSnapshot, Status, RowVersion, " +
        "IsDeleted, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy";

    private static Application Map(SqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        ClientId = r.GetGuid(1),
        PropertyId = r.GetGuid(2),
        PriceSnapshot = r.GetDecimal(3),
        PropertyTitleSnapshot = r.GetString(4),
        Status = r.GetString(5),
        RowVersion = (byte[])r.GetValue(6),
        IsDeleted = r.GetBoolean(7),
        CreatedAt = r.GetDateTime(8),
        CreatedBy = r.IsDBNull(9) ? null : r.GetGuid(9),
        UpdatedAt = r.IsDBNull(10) ? null : r.GetDateTime(10),
        UpdatedBy = r.IsDBNull(11) ? null : r.GetGuid(11)
    };

    private async Task<SqlCommand> CreateCommandAsync(string sql)
    {
        var conn = await session.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = session.Transaction;
        return cmd;
    }

    private static void P(SqlCommand cmd, string name, object? value)
    {
        var p = cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        if (value is decimal)
        {
            p.SqlDbType = SqlDbType.Decimal;
            p.Precision = 18;
            p.Scale = 2;
        }
    }

    public async Task<Application?> GetByIdAsync(Guid id)
    {
        await using var cmd = await CreateCommandAsync(
            $"SELECT {Columns} FROM dbo.Applications WHERE Id = @Id AND IsDeleted = 0");
        P(cmd, "@Id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<Application>> GetAllAsync()
    {
        var list = new List<Application>();
        await using var cmd = await CreateCommandAsync(
            $"SELECT {Columns} FROM dbo.Applications WHERE IsDeleted = 0 ORDER BY CreatedAt DESC");
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) list.Add(Map(reader));
        return list;
    }

    public async Task<PagedResult<Application>> GetPagedAsync(int page, int pageSize, string? status)
    {
        int total;
        await using (var countCmd = await CreateCommandAsync(
            "SELECT COUNT(*) FROM dbo.Applications WHERE IsDeleted = 0 AND (@Status IS NULL OR Status = @Status)"))
        {
            P(countCmd, "@Status", status);
            total = (int)(await countCmd.ExecuteScalarAsync())!;
        }

        var items = new List<Application>();
        await using var cmd = await CreateCommandAsync($@"
SELECT {Columns} FROM dbo.Applications
WHERE IsDeleted = 0 AND (@Status IS NULL OR Status = @Status)
ORDER BY CreatedAt DESC, Id
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
        P(cmd, "@Status", status);
        P(cmd, "@Offset", (page - 1) * pageSize);
        P(cmd, "@PageSize", pageSize);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) items.Add(Map(reader));

        return new PagedResult<Application> { Items = items, TotalCount = total };
    }

    public async Task<Guid> AddAsync(Application a)
    {
        if (a.Id == Guid.Empty) a.Id = Guid.NewGuid(); // GUID генерується на клієнті
        await using var cmd = await CreateCommandAsync(@"
INSERT INTO dbo.Applications
    (Id, ClientId, PropertyId, PriceSnapshot, PropertyTitleSnapshot, Status, IsDeleted, CreatedAt, CreatedBy)
VALUES
    (@Id, @ClientId, @PropertyId, @PriceSnapshot, @Title, @Status, 0, SYSUTCDATETIME(), @CreatedBy)");
        P(cmd, "@Id", a.Id);
        P(cmd, "@ClientId", a.ClientId);
        P(cmd, "@PropertyId", a.PropertyId);
        P(cmd, "@PriceSnapshot", a.PriceSnapshot);
        P(cmd, "@Title", a.PropertyTitleSnapshot);
        P(cmd, "@Status", a.Status);
        P(cmd, "@CreatedBy", a.CreatedBy);
        await cmd.ExecuteNonQueryAsync();
        return a.Id;
    }

    /// <summary>Оптимістична конкурентність: оновлення можливе лише з актуальним RowVersion.</summary>
    public async Task<bool> UpdateAsync(Application a)
    {
        await using var cmd = await CreateCommandAsync(@"
UPDATE dbo.Applications
SET Status = @Status, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy
WHERE Id = @Id AND IsDeleted = 0 AND RowVersion = @RowVersion");
        P(cmd, "@Status", a.Status);
        P(cmd, "@UpdatedBy", a.UpdatedBy);
        P(cmd, "@Id", a.Id);
        P(cmd, "@RowVersion", a.RowVersion);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>М'яке видалення: рядок лишається для звітності.</summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var cmd = await CreateCommandAsync(@"
UPDATE dbo.Applications
SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id AND IsDeleted = 0");
        P(cmd, "@Id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
