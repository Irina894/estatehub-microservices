using Catalog.Bll.Dtos;
using Catalog.Bll.Services;

namespace Catalog.Bll.Caching;

/// <summary>
/// Декоратор над <see cref="PropertyService"/>: cache-aside для читання й інвалідація при записі.
/// Один і той самий екземпляр обслуговує REST-контролер і gRPC-сервіс, тож кеш працює для обох протоколів.
/// Читання: L1 (пам'ять) → L2 (Redis) → БД. Запис: спершу операція в БД, потім синхронна інвалідація.
/// </summary>
public class CachedPropertyService(IPropertyService inner, ITwoLevelCache cache) : IPropertyService
{
    // Картка об'єкта читається часто, змінюється рідко: L1 коротший за L2
    private static readonly CacheTtl DetailsTtl = new(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
    // Списки залежать від фільтрів і застарівають швидше
    private static readonly CacheTtl ListTtl = new(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));

    // ---------- Читання ----------

    public async Task<PropertyDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        (await cache.GetOrCreateAsync<PropertyDetailsDto>(
            CatalogCacheKeys.Property(id), t => inner.GetByIdAsync(id, t), DetailsTtl, ct))!;

    public async Task<PagedResult<PropertyDto>> GetPagedAsync(PropertyQuery query, CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(CatalogCacheKeys.PropertiesScope, ct);
        return (await cache.GetOrCreateAsync<PagedResult<PropertyDto>>(
            CatalogCacheKeys.PropertiesList(version, query), t => inner.GetPagedAsync(query, t), ListTtl, ct))!;
    }

    public async Task<IReadOnlyList<PropertyDto>> GetFeaturedAsync(string city, int limit, CancellationToken ct = default)
    {
        var version = await cache.GetVersionAsync(CatalogCacheKeys.PropertiesScope, ct);
        return (await cache.GetOrCreateAsync<IReadOnlyList<PropertyDto>>(
            CatalogCacheKeys.Featured(version, city, limit), t => inner.GetFeaturedAsync(city, limit, t), ListTtl, ct))!;
    }

    // Рідкісний аналітичний запит: не кешуємо
    public Task<IReadOnlyList<PropertyDto>> GetByAmenitySinceAsync(string amenityName, DateTime since, CancellationToken ct = default) =>
        inner.GetByAmenitySinceAsync(amenityName, since, ct);

    // ---------- Запис: операція → синхронна інвалідація ----------

    public async Task<PropertyDetailsDto> CreateAsync(CreatePropertyDto dto, CancellationToken ct = default)
    {
        var created = await inner.CreateAsync(dto, ct);
        await cache.BumpVersionAsync(CatalogCacheKeys.PropertiesScope, "PropertyCreated", ct);   // новий об'єкт з'являється у списках
        return created;
    }

    public async Task UpdateAsync(Guid id, UpdatePropertyDto dto, CancellationToken ct = default)
    {
        await inner.UpdateAsync(id, dto, ct);
        await InvalidateAsync(id, "PropertyUpdated", ct);
    }

    public async Task ChangePriceAsync(Guid id, ChangePriceDto dto, CancellationToken ct = default)
    {
        await inner.ChangePriceAsync(id, dto, ct);
        await InvalidateAsync(id, "PriceChanged", ct);
    }

    public async Task ChangeStatusAsync(Guid id, ChangeStatusDto dto, CancellationToken ct = default)
    {
        await inner.ChangeStatusAsync(id, dto, ct);
        await InvalidateAsync(id, "StatusChanged", ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await InvalidateAsync(id, "PropertyDeleted", ct);
    }

    // Зручності (amenities) входять лише в картку об'єкта, а не в елементи списків
    public async Task AddAmenityAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default)
    {
        await inner.AddAmenityAsync(propertyId, amenityId, ct);
        await cache.RemoveAsync(CatalogCacheKeys.Property(propertyId), "AmenityAdded", ct);
    }

    public async Task RemoveAmenityAsync(Guid propertyId, Guid amenityId, CancellationToken ct = default)
    {
        await inner.RemoveAmenityAsync(propertyId, amenityId, ct);
        await cache.RemoveAsync(CatalogCacheKeys.Property(propertyId), "AmenityRemoved", ct);
    }

    /// <summary>Картка конкретного об'єкта + усі списки (через зміну версії групи).</summary>
    private async Task InvalidateAsync(Guid id, string reason, CancellationToken ct)
    {
        await cache.RemoveAsync(CatalogCacheKeys.Property(id), reason, ct);
        await cache.BumpVersionAsync(CatalogCacheKeys.PropertiesScope, reason, ct);
    }
}
