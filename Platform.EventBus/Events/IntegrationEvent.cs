namespace Platform.EventBus.Events;

/// <summary>
/// Базовий тип інтеграційної події: незмінний record. EventId - ключ ідемпотентності, CorrelationId - бізнес-трасування
/// (той самий X-Correlation-Id, що й у HTTP-запитах, Лаб. 05), SchemaVersion - версія схеми для зворотної сумісності.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }

    /// <summary>Нові поля додаємо, наявні не перейменовуємо й не видаляємо; при несумісній зміні - підвищуємо версію.</summary>
    public int SchemaVersion { get; init; } = 1;
}
