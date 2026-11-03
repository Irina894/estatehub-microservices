using Dapper;
using Deals.Domain.Interfaces;
using Deals.Domain.Models;

namespace Deals.Dal.Repositories;

/// <summary>
/// Спільна база для Dapper-репозиторіїв. Імена таблиць - константи в похідних класах,
/// усі значення передаються лише як SQL-параметри.
/// </summary>
public abstract class BaseDapperRepository<T>(DbSession session) : IGenericRepository<T> where T : EntityBase
{
    protected DbSession Session { get; } = session;

    protected abstract string TableName { get; }
    protected abstract string InsertSql { get; }
    protected abstract string UpdateSql { get; }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        var conn = await Session.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<T>(
            $"SELECT * FROM {TableName} WHERE Id = @id", new { id }, Session.Transaction);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync()
    {
        var conn = await Session.OpenAsync();
        var rows = await conn.QueryAsync<T>($"SELECT * FROM {TableName}", transaction: Session.Transaction);
        return rows.AsList();
    }

    public virtual async Task<Guid> AddAsync(T entity)
    {
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid(); // GUID генерується на клієнті
        var conn = await Session.OpenAsync();
        await conn.ExecuteAsync(InsertSql, entity, Session.Transaction);
        return entity.Id;
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        var conn = await Session.OpenAsync();
        return await conn.ExecuteAsync(UpdateSql, entity, Session.Transaction) > 0;
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var conn = await Session.OpenAsync();
        return await conn.ExecuteAsync(
            $"DELETE FROM {TableName} WHERE Id = @id", new { id }, Session.Transaction) > 0;
    }
}
