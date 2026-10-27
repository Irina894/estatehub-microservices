using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class AmenityRepository(CatalogDbContext context) : GenericRepository<Amenity>(context), IAmenityRepository
{
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await Set.AnyAsync(a => a.Name == name, ct);
}
