using Deals.Bll.DTOs;
using Deals.Bll.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Deals.Bll.Caching;

/// <summary>Декоратор: довідник рієлторів змінюється рідко, тому читання кешуються в пам'яті.</summary>
public class CachedRealtorService(IRealtorService inner, IMemoryCache cache) : IRealtorService
{
    private const string AllKey = "realtors:all";
    private static string Key(Guid id) => $"realtors:{id}";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    public async Task<RealtorDto> CreateAsync(CreateRealtorDto dto)
    {
        var result = await inner.CreateAsync(dto);
        cache.Remove(AllKey);
        return result;
    }

    public async Task<RealtorDto> GetAsync(Guid id) =>
        (await cache.GetOrCreateAsync(Key(id), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;
            return inner.GetAsync(id);
        }))!;

    public async Task<IReadOnlyList<RealtorDto>> GetAllAsync() =>
        (await cache.GetOrCreateAsync(AllKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;
            return inner.GetAllAsync();
        }))!;

    public async Task<RealtorDto> UpdateAsync(Guid id, UpdateRealtorDto dto)
    {
        var result = await inner.UpdateAsync(id, dto);
        Invalidate(id);
        return result;
    }

    public async Task DeactivateAsync(Guid id)
    {
        await inner.DeactivateAsync(id);
        Invalidate(id);
    }

    private void Invalidate(Guid id)
    {
        cache.Remove(Key(id));
        cache.Remove(AllKey);
    }
}
