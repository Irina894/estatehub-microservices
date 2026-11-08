using Catalog.Domain.Entities;

namespace Catalog.Dal.Repositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    /// <summary>EXPLICIT LOADING: категорія + її об'єкти окремим запитом (2 SQL-запити).</summary>
    Task<Category?> GetWithPropertiesAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
}
