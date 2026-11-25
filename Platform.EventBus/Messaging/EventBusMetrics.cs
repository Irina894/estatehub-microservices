using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Platform.EventBus.Messaging;

/// <summary>
/// Метрики асинхронної обробки (Лаб. 7 + 8): опубліковано / оброблено (за результатом) і глибина DLQ (gauge).
/// Теги мають низьку кардинальність: тип події, результат, назва черги.
/// </summary>
public sealed class EventBusMetrics
{
    public const string MeterName = "Platform.Messaging";

    private readonly Counter<long> _published;
    private readonly Counter<long> _processed;
    private readonly ConcurrentDictionary<string, long> _dlqDepth = new();

    public EventBusMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _published = meter.CreateCounter<long>("messaging.events.published", "{event}", "Опубліковані події (тег event_type)");
        _processed = meter.CreateCounter<long>("messaging.events.processed", "{event}",
            "Оброблені повідомлення (теги queue, outcome = ack | retry | dead_letter)");

        meter.CreateObservableGauge("messaging.dlq.depth",
            () => _dlqDepth.Select(kv => new Measurement<long>(kv.Value, new KeyValuePair<string, object?>("queue", kv.Key))),
            "{message}", "Кількість повідомлень у dead letter queue");
    }

    public void Published(string eventType) =>
        _published.Add(1, new KeyValuePair<string, object?>("event_type", eventType));

    public void Processed(string queue, string outcome) =>
        _processed.Add(1, new("queue", queue), new("outcome", outcome));

    public void SetDlqDepth(string queue, long depth) => _dlqDepth[queue] = depth;
}
