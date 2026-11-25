using Deals.Bll.DTOs;
using Deals.Bll.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Deals.Bll.Caching;

/// <summary>
/// Декоратор: довідник рієлторів змінюється рідко. З Лаб. 2 кеш був лише в пам'яті; тепер він дворівневий
/// (L1 → Redis → БД) і спільний для всіх екземплярів сервісу.
/// </summary>
public class CachedRealtorService(IRealtorService inner, ITwoLevelCache cache) : IRealtorService
{
    private static readonly CacheTtl ReferenceTtl =
        new(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10), CacheItemPriority.High);

    public async Task<RealtorDto> GetAsync(Guid id) =>
        (await cache.GetOrCreateAsync<RealtorDto>(
            DealsCacheKeys.Realtor(id), _ => inner.GetAsync(id), ReferenceTtl, CancellationToken.None))!;

    public async Task<IReadOnlyList<RealtorDto>> GetAllAsync() =>
        (await cache.GetOrCreateAsync<IReadOnlyList<RealtorDto>>(
            DealsCacheKeys.RealtorsAll, _ => inner.GetAllAsync(), ReferenceTtl, CancellationToken.None))!;

    public async Task<RealtorDto> CreateAsync(CreateRealtorDto dto)
    {
        var result = await inner.CreateAsync(dto);
        await cache.RemoveAsync(DealsCacheKeys.RealtorsAll, "RealtorCreated", CancellationToken.None);
        return result;
    }

    public async Task<RealtorDto> UpdateAsync(Guid id, UpdateRealtorDto dto)
    {
        var result = await inner.UpdateAsync(id, dto);
        await InvalidateAsync(id, "RealtorUpdated");
        return result;
    }

    public async Task DeactivateAsync(Guid id)
    {
        await inner.DeactivateAsync(id);
        await InvalidateAsync(id, "RealtorDeactivated");
    }

    private async Task InvalidateAsync(Guid id, string reason)
    {
        await cache.RemoveAsync(DealsCacheKeys.Realtor(id), reason, CancellationToken.None);
        await cache.RemoveAsync(DealsCacheKeys.RealtorsAll, reason, CancellationToken.None);
    }
}
