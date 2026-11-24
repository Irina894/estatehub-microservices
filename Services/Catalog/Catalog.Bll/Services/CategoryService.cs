using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Dal.Repositories;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;

namespace Catalog.Bll.Services;

public class CategoryService(IUnitOfWork uow, IMapper mapper) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
        => mapper.Map<List<CategoryDto>>(await uow.Categories.GetAllAsync(ct));

    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await uow.Categories.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);
        return mapper.Map<CategoryDto>(category);
    }

    // Explicit Loading (2 SQL-запити)
    public async Task<CategoryWithPropertiesDto> GetWithPropertiesAsync(Guid id, CancellationToken ct = default)
    {
        var category = await uow.Categories.GetWithPropertiesAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);
        return mapper.Map<CategoryWithPropertiesDto>(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        var name = dto.Name.Trim();
        if (await uow.Categories.ExistsByNameAsync(name, ct))
            throw new ConflictException($"Категорія '{name}' уже існує.");

        var category = new Category(name);
        await uow.Categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<CategoryDto>(category);
    }
}
