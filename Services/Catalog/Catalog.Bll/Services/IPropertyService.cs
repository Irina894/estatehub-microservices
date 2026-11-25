using Catalog.Bll.Dtos;

namespace Catalog.Bll.Services;

public interface IPropertyService
{
    Task<PagedResult<PropertyDto>> GetPagedAsync(PropertyQuery query, CancellationToken ct = default);
    Task<PropertyDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PropertyDetailsDto> CreateAsync(CreatePropertyDto dto, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdatePropertyDto dto, CancellationToken ct = default);
    Task ChangePriceAsync(Guid id, ChangePriceDto dto, CancellationToken ct = default);
    Task ChangeStatusAsync(Guid id, ChangeStatusDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task AddAmenityAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default);
    Task RemoveAmenityAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default);
    Task<IReadOnlyList<PropertyDto>> GetByAmenitySinceAsync(string amenityName, DateTime since, CancellationToken ct = default);
    Task<IReadOnlyList<PropertyDto>> GetFeaturedAsync(string city, int limit, CancellationToken ct = default);
}
