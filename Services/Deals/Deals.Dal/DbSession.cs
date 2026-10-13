using System.Data;
using Microsoft.Data.SqlClient;

namespace Deals.Dal;

/// <summary>
/// Одне SqlConnection і (за потреби) одна SqlTransaction на scope запиту.
/// Усі репозиторії працюють через цей об'єкт, тому транзакція спільна.
/// </summary>
public sealed class DbSession(string connectionString) : IAsyncDisposable
{
    public SqlConnection Connection { get; } = new(connectionString);
    public SqlTransaction? Transaction { get; private set; }

    public async Task<SqlConnection> OpenAsync()
    {
        if (Connection.State != ConnectionState.Open)
            await Connection.OpenAsync();
        return Connection;
    }

    public async Task BeginAsync()
    {
        if (Transaction is not null)
            throw new InvalidOperationException("Transaction is already active.");
        await OpenAsync();
        Transaction = (SqlTransaction)await Connection.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (Transaction is null) throw new InvalidOperationException("No active transaction.");
        await Transaction.CommitAsync();
        await Transaction.DisposeAsync();
        Transaction = null;
    }

    public async Task RollbackAsync()
    {
        if (Transaction is null) return;
        try { await Transaction.RollbackAsync(); }
        finally
        {
            await Transaction.DisposeAsync();
            Transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Transaction is not null) await RollbackAsync();
        await Connection.DisposeAsync();
    }
}
