using Catalog.Domain.Entities;

namespace Catalog.Dal.Repositories;

public interface IAmenityRepository : IGenericRepository<Amenity>
{
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
}
