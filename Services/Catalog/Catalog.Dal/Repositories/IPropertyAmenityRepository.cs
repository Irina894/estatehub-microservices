using Catalog.Domain.Entities;

namespace Catalog.Dal.Repositories;

public interface IPropertyAmenityRepository : IGenericRepository<PropertyAmenity>
{
    /// <summary>LINQ через проміжну сутність: об'єкти зі зручністю X, додані після вказаної дати.</summary>
    Task<IReadOnlyList<Property>> GetRecentlyAddedAsync(string amenityName, DateTime since, CancellationToken ct = default);
    Task<PropertyAmenity?> GetLinkAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default);
}
