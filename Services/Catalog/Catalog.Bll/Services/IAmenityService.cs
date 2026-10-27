using Catalog.Bll.Dtos;

namespace Catalog.Bll.Services;

public interface IAmenityService
{
    Task<IReadOnlyList<AmenityDto>> GetAllAsync(CancellationToken ct = default);
    Task<AmenityDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AmenityDto> CreateAsync(CreateAmenityDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
