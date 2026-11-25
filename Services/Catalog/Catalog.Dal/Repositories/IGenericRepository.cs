namespace Catalog.Dal.Repositories;

/// <summary>Узагальнений асинхронний репозиторій. SaveChanges тут НЕ викликається — це робота Unit of Work.</summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
}
