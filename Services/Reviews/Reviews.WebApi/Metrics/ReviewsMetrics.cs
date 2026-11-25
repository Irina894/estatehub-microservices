using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Reviews.WebApi.Metrics;

/// <summary>
/// Лаб. 7: бізнес-метрики сервісу Reviews (IMeterFactory, Singleton, теги з низькою кардинальністю).
/// Meter "Reviews.Api" підключено через AddMeter у ServiceDefaults.
/// </summary>
public sealed class ReviewsMetrics
{
    public const string MeterName = "Reviews.Api";

    private readonly Counter<long> _created;
    private readonly Counter<long> _deleted;
    private readonly Counter<long> _comments;
    private readonly Histogram<double> _writeDuration;
    private int _inFlight;

    public ReviewsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _created = meter.CreateCounter<long>("reviews.created", "{review}", "Створені відгуки (тег rating = 1..5)");
        _deleted = meter.CreateCounter<long>("reviews.deleted", "{review}", "Видалені відгуки");
        _comments = meter.CreateCounter<long>("reviews.comments.added", "{comment}", "Додані коментарі");
        _writeDuration = meter.CreateHistogram<double>(
            "reviews.write.duration", "ms", "Тривалість операцій запису (тег operation)");

        meter.CreateObservableGauge(
            "reviews.writes.in_flight", () => Volatile.Read(ref _inFlight),
            "{operation}", "Операції запису, що виконуються зараз");
    }

    /// <summary>rating обмежуємо 1..5, тож тег має щонайбільше 5 значень.</summary>
    public void ReviewCreated(int rating) =>
        _created.Add(1, new KeyValuePair<string, object?>("rating", Math.Clamp(rating, 1, 5)));

    public void ReviewDeleted() => _deleted.Add(1);
    public void CommentAdded() => _comments.Add(1);

    /// <summary>operation: "create" | "update" | "comment" | "delete".</summary>
    public IDisposable MeasureWrite(string operation) => new WriteScope(this, operation);

    private sealed class WriteScope : IDisposable
    {
        private readonly ReviewsMetrics _owner;
        private readonly string _operation;
        private readonly long _start = Stopwatch.GetTimestamp();

        public WriteScope(ReviewsMetrics owner, string operation)
        {
            _owner = owner;
            _operation = operation;
            Interlocked.Increment(ref owner._inFlight);
        }

        public void Dispose()
        {
            Interlocked.Decrement(ref _owner._inFlight);
            _owner._writeDuration.Record(
                Stopwatch.GetElapsedTime(_start).TotalMilliseconds,
                new KeyValuePair<string, object?>("operation", _operation));
        }
    }
}
