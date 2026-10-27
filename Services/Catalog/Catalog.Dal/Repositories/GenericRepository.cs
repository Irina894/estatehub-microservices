using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class GenericRepository<T>(CatalogDbContext context) : IGenericRepository<T> where T : class
{
    protected readonly CatalogDbContext Context = context;
    protected DbSet<T> Set => Context.Set<T>();

    // FindAsync шукає спочатку в трекері, потім у БД (глобальні фільтри НЕ застосовує — тому PropertyRepository перевизначає)
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Set.FindAsync(new object[] { id }, ct);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await Set.AsNoTracking().ToListAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await Set.AddAsync(entity, ct);

    public virtual void Update(T entity) => Set.Update(entity);

    public virtual void Delete(T entity) => Set.Remove(entity);
}
