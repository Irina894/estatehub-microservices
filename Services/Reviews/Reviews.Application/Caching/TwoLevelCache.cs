using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Caching;

/// <summary>Терміни життя двох рівнів. L1 завжди не довший за L2, інакше застарілі дані в пам'яті переживуть оновлення в Redis.</summary>
public readonly record struct CacheTtl(TimeSpan L1, TimeSpan L2, CacheItemPriority Priority = CacheItemPriority.Normal);

/// <summary>Дворівневий кеш: L1 (IMemoryCache) → L2 (Redis через IDistributedCache) → джерело даних (БД / інший сервіс).</summary>
public interface ITwoLevelCache
{
    /// <summary>Cache-aside: L1 → L2 → factory; кожен рівень заповнюється на зворотному шляху.</summary>
    Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, CacheTtl ttl, CancellationToken ct)
        where T : class;

    /// <summary>Видаляє ключ з обох рівнів (синхронно з операцією запису).</summary>
    Task RemoveAsync(string key, string reason, CancellationToken ct);

    /// <summary>Версія групи ключів (для масової інвалідації списків): ключ списку містить версію.</summary>
    Task<string> GetVersionAsync(string scope, CancellationToken ct);

    /// <summary>Змінює версію групи, і всі ключі з попередньою версією перестають використовуватися.</summary>
    Task BumpVersionAsync(string scope, string reason, CancellationToken ct);
}

public sealed class TwoLevelCache(
    IMemoryCache memory, IDistributedCache distributed, CacheMetrics metrics, ILogger<TwoLevelCache> logger)
    : ITwoLevelCache
{
    private static readonly ActivitySource Source = new("EstateHub.Cache");
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly TimeSpan L2Timeout = TimeSpan.FromMilliseconds(500);   // Redis не має гальмувати запит
    private static readonly TimeSpan VersionL1Ttl = TimeSpan.FromSeconds(5);
    private const long L2RetryAfterMs = 15_000;                                    // «автомат»: після збою L2 пропускаємо

    private readonly ConcurrentDictionary<string, string> _localVersions = new();
    private long _l2SkipUntil;

    public async Task<T?> GetOrCreateAsync<T>(
        string key, Func<CancellationToken, Task<T?>> factory, CacheTtl ttl, CancellationToken ct) where T : class
    {
        using var activity = Source.StartActivity("cache.get_or_create");
        activity?.SetTag("cache.key", key);

        // ---- L1: пам'ять ----
        if (memory.TryGetValue(key, out T? fromMemory) && fromMemory is not null)
        {
            metrics.Hit("l1");
            activity?.SetTag("cache.result", "l1_hit");
            logger.LogDebug("Cache hit L1 {Key}, ttl {Ttl}", key, ttl.L1);
            return fromMemory;
        }
        metrics.Miss("l1");

        // ---- L2: Redis ----
        var bytes = await L2GetAsync(key, ct);
        if (bytes is not null)
        {
            var fromRedis = JsonSerializer.Deserialize<T>(bytes, Json);
            if (fromRedis is not null)
            {
                metrics.Hit("l2");
                activity?.SetTag("cache.result", "l2_hit");
                logger.LogDebug("Cache hit L2 {Key}, size {Size} bytes", key, bytes.Length);
                SetMemory(key, fromRedis, ttl);   // заповнюємо L1 на зворотному шляху
                return fromRedis;
            }
        }
        metrics.Miss("l2");
        activity?.SetTag("cache.result", "miss");
        logger.LogDebug("Cache miss {Key}", key);

        // ---- Джерело даних ----
        var created = await factory(ct);
        if (created is null) return null;

        var payload = JsonSerializer.SerializeToUtf8Bytes(created, Json);
        await L2SetAsync(key, payload, ttl.L2, ct);
        SetMemory(key, created, ttl);
        logger.LogDebug("Cache filled {Key}, size {Size} bytes, ttl L1 {L1} / L2 {L2}", key, payload.Length, ttl.L1, ttl.L2);
        return created;
    }

    public async Task RemoveAsync(string key, string reason, CancellationToken ct)
    {
        memory.Remove(key);
        await L2RemoveAsync(key, ct);
        metrics.Invalidation(reason);
        logger.LogInformation("Cache invalidated {Key}, reason {Reason}", key, reason);
    }

    public async Task<string> GetVersionAsync(string scope, CancellationToken ct)
    {
        var key = $"{scope}:ver";
        if (memory.TryGetValue(key, out string? cached) && cached is not null) return cached;

        var bytes = await L2GetAsync(key, ct);
        var version = bytes is not null
            ? Encoding.UTF8.GetString(bytes)
            : (_localVersions.TryGetValue(scope, out var local) ? local : "0");   // Redis недоступний/порожній: остання версія цього процесу

        memory.Set(key, version, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = VersionL1Ttl });
        return version;
    }

    public async Task BumpVersionAsync(string scope, string reason, CancellationToken ct)
    {
        var key = $"{scope}:ver";
        var version = DateTime.UtcNow.Ticks.ToString();
        _localVersions[scope] = version;
        memory.Set(key, version, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = VersionL1Ttl });
        await L2SetAsync(key, Encoding.UTF8.GetBytes(version), TimeSpan.FromDays(1), ct);
        metrics.Invalidation(reason);
        logger.LogInformation("Cache scope {Scope} invalidated (new version {Version}), reason {Reason}", scope, version, reason);
    }

    // ---------- L1 ----------

    private void SetMemory<T>(string key, T value, CacheTtl ttl) where T : class
    {
        var options = new MemoryCacheEntryOptions
        {
            Size = 1,                                                  // обов'язково, бо в AddMemoryCache задано SizeLimit
            Priority = ttl.Priority,                                   // що витісняти першим, коли кеш заповнений
            AbsoluteExpirationRelativeToNow = Min(ttl.L1, ttl.L2)      // абсолютний термін: він обмежує застарілість
        };
        options.RegisterPostEvictionCallback(
            static (_, _, reason, state) => ((CacheMetrics)state!).Eviction(reason.ToString()), metrics);
        memory.Set(key, value, options);
    }

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;

    // ---------- L2 (Redis): будь-який збій = «L2 недоступний», сервіс іде в БД ----------

    private bool L2Available => Environment.TickCount64 >= Interlocked.Read(ref _l2SkipUntil);

    private void MarkL2Down(string operation, Exception ex)
    {
        Interlocked.Exchange(ref _l2SkipUntil, Environment.TickCount64 + L2RetryAfterMs);
        metrics.Error(operation);
        logger.LogWarning(ex, "Redis (L2) unavailable during {Operation}; skipping L2 for {Seconds} s", operation, L2RetryAfterMs / 1000);
    }

    private async Task<byte[]?> L2GetAsync(string key, CancellationToken ct)
    {
        if (!L2Available) return null;
        var started = Stopwatch.GetTimestamp();
        try
        {
            var bytes = await distributed.GetAsync(key, ct).WaitAsync(L2Timeout, ct);
            metrics.Duration("l2", Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return bytes;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }   // скасування клієнтом не ковтаємо
        catch (Exception ex) { MarkL2Down("get", ex); return null; }
    }

    private async Task L2SetAsync(string key, byte[] payload, TimeSpan ttl, CancellationToken ct)
    {
        if (!L2Available) return;
        try
        {
            await distributed.SetAsync(key, payload,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct).WaitAsync(L2Timeout, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { MarkL2Down("set", ex); }
    }

    private async Task L2RemoveAsync(string key, CancellationToken ct)
    {
        if (!L2Available) return;
        try { await distributed.RemoveAsync(key, ct).WaitAsync(L2Timeout, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { MarkL2Down("remove", ex); }
    }
}
