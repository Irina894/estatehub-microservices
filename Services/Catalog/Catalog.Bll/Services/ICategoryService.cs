using Catalog.Bll.Dtos;

namespace Catalog.Bll.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CategoryWithPropertiesDto> GetWithPropertiesAsync(Guid id, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default);
}
