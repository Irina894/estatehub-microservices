using System.Diagnostics.Metrics;

namespace Deals.Bll.Caching;

/// <summary>
/// Власні метрики кешу (Meter "Deals.Cache"). Видно в Aspire Dashboard → Metrics:
/// cache.hits / cache.misses (тег cache.level = l1 | l2), cache.invalidations, cache.evictions, cache.errors, cache.duration.
/// </summary>
public sealed class CacheMetrics
{
    public const string MeterName = "Deals.Cache";

    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Counter<long> _invalidations;
    private readonly Counter<long> _evictions;
    private readonly Counter<long> _errors;
    private readonly Histogram<double> _duration;

    public CacheMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _hits = meter.CreateCounter<long>("cache.hits", description: "Попадання в кеш");
        _misses = meter.CreateCounter<long>("cache.misses", description: "Промахи кешу");
        _invalidations = meter.CreateCounter<long>("cache.invalidations", description: "Інвалідації кешу");
        _evictions = meter.CreateCounter<long>("cache.evictions", description: "Витіснення з кешу в пам'яті");
        _errors = meter.CreateCounter<long>("cache.errors", description: "Збої Redis (L2)");
        _duration = meter.CreateHistogram<double>("cache.duration", unit: "ms", description: "Тривалість операцій з кешем");
    }

    public void Hit(string level) => _hits.Add(1, new KeyValuePair<string, object?>("cache.level", level));
    public void Miss(string level) => _misses.Add(1, new KeyValuePair<string, object?>("cache.level", level));
    public void Invalidation(string reason) => _invalidations.Add(1, new KeyValuePair<string, object?>("cache.reason", reason));
    public void Eviction(string reason) => _evictions.Add(1, new KeyValuePair<string, object?>("cache.reason", reason));
    public void Error(string operation) => _errors.Add(1, new KeyValuePair<string, object?>("cache.operation", operation));
    public void Duration(string level, double milliseconds) =>
        _duration.Record(milliseconds, new KeyValuePair<string, object?>("cache.level", level));
}
