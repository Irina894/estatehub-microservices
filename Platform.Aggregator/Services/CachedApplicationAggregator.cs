using Platform.Aggregator.Caching;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Services;

/// <summary>
/// Кеш агрегованої відповіді (L1 → Redis). Кеш перевіряється ДО паралельних gRPC-викликів, тож влучання не звертається
/// до жодного сервісу. Часткові відповіді (з warnings) не кешуються, інакше тимчасовий збій «застряг» би в кеші.
/// TTL агрегата коротший, ніж у кешів окремих сервісів: агрегат не має виглядати «свіжішим» за дані, з яких зібраний.
/// </summary>
public class CachedApplicationAggregator(
    GrpcApplicationAggregationService inner, ITwoLevelCache cache) : IApplicationAggregator
{
    private static readonly CacheTtl Ttl = new(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

    public static string Key(Guid applicationId) => $"aggregator:application:{applicationId}:full";

    public async Task<FullApplicationDto?> GetFullApplicationAsync(Guid applicationId, CancellationToken ct)
    {
        FullApplicationDto? fresh = null;

        var result = await cache.GetOrCreateAsync<FullApplicationDto>(
            Key(applicationId),
            async t =>
            {
                fresh = await inner.GetFullApplicationAsync(applicationId, t);
                // null від фабрики = «не кешувати»: так пропускаємо відсутню заявку й часткові відповіді
                return fresh is { Warnings.Count: 0 } ? fresh : null;
            },
            Ttl, ct);

        return result ?? fresh;
    }

    /// <summary>Явна інвалідація (наприклад, після зміни заявки): видаляє агрегат з обох рівнів.</summary>
    public Task InvalidateAsync(Guid applicationId, CancellationToken ct) =>
        cache.RemoveAsync(Key(applicationId), "ManualInvalidation", ct);
}
