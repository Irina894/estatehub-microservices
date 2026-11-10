using MediatR;

namespace Reviews.Application.Caching;

/// <summary>
/// Cache-aside для запитів CQRS: L1 → L2 (Redis) → handler (MongoDB). Застосовується лише до запитів з <see cref="ICachedQuery"/>:
/// для інших запитів DI просто не створює цей behavior (не виконується обмеження типу).
/// </summary>
public sealed class QueryCachingBehavior<TRequest, TResponse>(ITwoLevelCache cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ICachedQuery
    where TResponse : class
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var version = request.UsesListVersion
            ? await cache.GetVersionAsync(ReviewsCacheKeys.ListScope, ct)
            : string.Empty;

        return (await cache.GetOrCreateAsync<TResponse>(request.BuildKey(version), _ => next(), request.Ttl, ct))!;
    }
}

/// <summary>
/// Інвалідація кешу одразу після успішного запису (синхронно з операцією: клієнт не побачить застарілих даних).
/// Якщо команда впала, handler кине виняток, і кеш не чіпається.
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>(ITwoLevelCache cache)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ICacheInvalidating
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();

        foreach (var key in request.KeysToRemove)
            await cache.RemoveAsync(key, request.Reason, ct);
        if (request.InvalidatesLists)
            await cache.BumpVersionAsync(ReviewsCacheKeys.ListScope, request.Reason, ct);

        return response;
    }
}
