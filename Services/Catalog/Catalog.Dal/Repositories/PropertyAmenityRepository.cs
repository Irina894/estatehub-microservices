using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class PropertyAmenityRepository(CatalogDbContext context)
    : GenericRepository<PropertyAmenity>(context), IPropertyAmenityRepository
{
    // LINQ to Entities ЧЕРЕЗ проміжну сутність PropertyAmenity:
    // фільтр по Amenity.Name і по власному полю зв'язку AddedAt.
    // Include після Select(pa => pa.Property) EF не дозволяє, тому Category беремо тією ж проєкцією:
    // у tracking-запиті EF сам заповнює Property.Category (navigation fix-up).
    public async Task<IReadOnlyList<Property>> GetRecentlyAddedAsync(string amenityName, DateTime since, CancellationToken ct = default)
    {
        var rows = await Set
            .Where(pa => pa.Amenity.Name == amenityName && pa.AddedAt >= since)
            .OrderByDescending(pa => pa.AddedAt)
            .Select(pa => new { pa.Property, pa.Property.Category })
            .ToListAsync(ct);

        return rows.Select(r => r.Property).ToList();
    }

    public async Task<PropertyAmenity?> GetLinkAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default)
        => await Set.FirstOrDefaultAsync(pa => pa.PropertyId == propertyId && pa.AmenityId == amenityId, ct);

    // У проміжної сутності складений ключ, тому пошук за одним Guid не має сенсу
    public override Task<PropertyAmenity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => throw new NotSupportedException("Використовуйте GetLinkAsync(propertyId, amenityId).");
}