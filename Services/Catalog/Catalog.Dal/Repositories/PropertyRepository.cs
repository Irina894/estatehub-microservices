using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class PropertyRepository(CatalogDbContext context) : GenericRepository<Property>(context), IPropertyRepository
{
    // Eager Loading: Include / ThenInclude. AsSingleQuery -> один SQL із JOIN-ами.
    public async Task<Property?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(p => p.Category)
            .Include(p => p.Location)
            .Include(p => p.Images)
            .Include(p => p.PropertyAmenities)
                .ThenInclude(pa => pa.Amenity)
            .AsSingleQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    // FindAsync ігнорує query filter (IsDeleted) -> використовуємо FirstOrDefault
    public override async Task<Property?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(p => p.Id == id, ct);

    // Specification pattern: фільтр/сортування/пагінація виконуються в SQL
    public async Task<IReadOnlyList<Property>> ListAsync(ISpecification<Property> spec, CancellationToken ct = default)
        => await Set.WithSpecification(spec).ToListAsync(ct);

    public async Task<int> CountAsync(ISpecification<Property> spec, CancellationToken ct = default)
        => await Set.WithSpecification(spec).CountAsync(ct);

    public async Task<bool> ExistsByOwnerAndTitleAsync(Guid ownerId, string title, Guid? excludeId = null, CancellationToken ct = default)
        => await Set.AnyAsync(p => p.OwnerId == ownerId && p.Title == title && (excludeId == null || p.Id != excludeId), ct);
}
