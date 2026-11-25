using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Catalog.Api.Metrics;

/// <summary>
/// Лаб. 7: бізнес-метрики сервісу Catalog (IMeterFactory, Singleton, теги з низькою кардинальністю).
/// Meter "Catalog.Api" підключено через AddMeter у ServiceDefaults.
/// </summary>
public sealed class CatalogMetrics
{
    public const string MeterName = "Catalog.Api";

    private static readonly HashSet<string> KnownStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "available", "reserved", "sold", "rented" };

    private readonly Counter<long> _created;
    private readonly Counter<long> _priceChanges;
    private readonly Counter<long> _statusChanges;
    private readonly Histogram<double> _searchDuration;
    private readonly Histogram<long> _searchTotal;
    private int _searchesInFlight;

    public CatalogMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _created = meter.CreateCounter<long>("catalog.properties.created", "{property}", "Створені об'єкти нерухомості");
        _priceChanges = meter.CreateCounter<long>("catalog.properties.price_changes", "{change}", "Зміни ціни об'єктів");
        _statusChanges = meter.CreateCounter<long>("catalog.properties.status_changes", "{change}", "Зміни статусу (тег to_status)");

        _searchDuration = meter.CreateHistogram<double>(
            "catalog.search.duration", "ms", "Тривалість пошуку/списку об'єктів (тег endpoint)");
        _searchTotal = meter.CreateHistogram<long>(
            "catalog.search.total_count", "{property}", "Скільки об'єктів знайдено запитом (розподіл)");

        meter.CreateObservableGauge(
            "catalog.searches.in_flight", () => Volatile.Read(ref _searchesInFlight),
            "{search}", "Пошукові запити, що виконуються зараз");
    }

    public void PropertyCreated() => _created.Add(1);
    public void PriceChanged() => _priceChanges.Add(1);

    public void StatusChanged(string? toStatus) =>
        _statusChanges.Add(1, new KeyValuePair<string, object?>(
            "to_status", toStatus is not null && KnownStatuses.Contains(toStatus) ? toStatus.ToLowerInvariant() : "other"));

    public void SearchFound(int totalCount) => _searchTotal.Record(totalCount);

    /// <summary>endpoint: "list" | "featured" | "by_amenity" (фіксований набір).</summary>
    public IDisposable MeasureSearch(string endpoint) => new SearchScope(this, endpoint);

    private sealed class SearchScope : IDisposable
    {
        private readonly CatalogMetrics _owner;
        private readonly string _endpoint;
        private readonly long _start = Stopwatch.GetTimestamp();

        public SearchScope(CatalogMetrics owner, string endpoint)
        {
            _owner = owner;
            _endpoint = endpoint;
            Interlocked.Increment(ref owner._searchesInFlight);
        }

        public void Dispose()
        {
            Interlocked.Decrement(ref _owner._searchesInFlight);
            _owner._searchDuration.Record(
                Stopwatch.GetElapsedTime(_start).TotalMilliseconds,
                new KeyValuePair<string, object?>("endpoint", _endpoint));
        }
    }
}
