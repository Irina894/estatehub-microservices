using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Bll.Caching;

/// <summary>
/// Категорії - довідник: змінюється дуже рідко, читається постійно, тому довгі TTL і пріоритет High
/// (при заповненні кешу такі записи витісняються в останню чергу).
/// </summary>
public class CachedCategoryService(ICategoryService inner, ITwoLevelCache cache) : ICategoryService
{
    private static readonly CacheTtl ReferenceTtl =
        new(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), CacheItemPriority.High);

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default) =>
        (await cache.GetOrCreateAsync<IReadOnlyList<CategoryDto>>(
            CatalogCacheKeys.CategoriesAll, t => inner.GetAllAsync(t), ReferenceTtl, ct))!;

    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        (await cache.GetOrCreateAsync<CategoryDto>(
            CatalogCacheKeys.Category(id), t => inner.GetByIdAsync(id, t), ReferenceTtl, ct))!;

    // Містить список об'єктів, які змінюються частіше, ніж довідник: не кешуємо
    public Task<CategoryWithPropertiesDto> GetWithPropertiesAsync(Guid id, CancellationToken ct = default) =>
        inner.GetWithPropertiesAsync(id, ct);

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        var created = await inner.CreateAsync(dto, ct);
        await cache.RemoveAsync(CatalogCacheKeys.CategoriesAll, "CategoryCreated", ct);
        return created;
    }
}
