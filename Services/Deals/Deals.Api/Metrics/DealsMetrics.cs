using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Deals.Api.Metrics;

/// <summary>
/// Лаб. 7: бізнес-метрики сервісу Deals. Meter створюється через IMeterFactory (DI керує часом життя
/// і ізолює метрики в тестах); клас реєструється як Singleton. Назва Meter "Deals.Api" додана
/// до AddMeter(...) у ServiceDefaults, тому метрики потрапляють у вкладку Metrics Aspire Dashboard.
/// Усі теги мають НИЗЬКУ кардинальність (скінченний набір значень): ніяких Id, e-mail чи вільного тексту.
/// </summary>
public sealed class DealsMetrics
{
    public const string MeterName = "Deals.Api";

    private static readonly HashSet<string> KnownStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "new", "approved", "rejected", "completed", "cancelled" };

    private readonly Counter<long> _applicationsCreated;
    private readonly Counter<long> _statusChanges;
    private readonly Histogram<double> _operationDuration;
    private int _inFlight;

    public DealsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _applicationsCreated = meter.CreateCounter<long>(
            "deals.applications.created", unit: "{application}", description: "Кількість створених заявок");

        _statusChanges = meter.CreateCounter<long>(
            "deals.applications.status_changes", unit: "{change}", description: "Зміни статусу заявок (тег to_status)");

        _operationDuration = meter.CreateHistogram<double>(
            "deals.operation.duration", unit: "ms", description: "Тривалість бізнес-транзакцій (тег operation)");

        // Gauge «знімок»: скільки транзакцій виконується просто зараз (callback читає лічильник без звернень до БД)
        meter.CreateObservableGauge(
            "deals.operations.in_flight", () => Volatile.Read(ref _inFlight),
            unit: "{operation}", description: "Бізнес-транзакції, що виконуються зараз");
    }

    public void ApplicationCreated() => _applicationsCreated.Add(1);

    public void StatusChanged(string? toStatus) =>
        _statusChanges.Add(1, new KeyValuePair<string, object?>(
            "to_status", toStatus is not null && KnownStatuses.Contains(toStatus) ? toStatus.ToLowerInvariant() : "other"));

    /// <summary>Вимірює транзакцію: збільшує in-flight, а при Dispose записує тривалість у гістограму.</summary>
    public IDisposable MeasureOperation(string operation) => new OperationScope(this, operation);

    private sealed class OperationScope : IDisposable
    {
        private readonly DealsMetrics _owner;
        private readonly string _operation;
        private readonly long _start = Stopwatch.GetTimestamp();

        public OperationScope(DealsMetrics owner, string operation)
        {
            _owner = owner;
            _operation = operation;
            Interlocked.Increment(ref owner._inFlight);
        }

        public void Dispose()
        {
            Interlocked.Decrement(ref _owner._inFlight);
            _owner._operationDuration.Record(
                Stopwatch.GetElapsedTime(_start).TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", _operation));
        }
    }
}
