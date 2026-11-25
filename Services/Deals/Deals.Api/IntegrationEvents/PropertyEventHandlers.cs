using Platform.EventBus.Events;
using Platform.EventBus.Messaging;

namespace Deals.Api.IntegrationEvents;

// По одному обробнику на тип події. Усі ідемпотентні: PropertySnapshotStore.ApplyOnceAsync перевіряє EventId
// (ProcessedEvents) і застосовує зміну в тій самій транзакції. Зміни копії - read model для запитів Deals.

public sealed class PropertyCreatedHandler(PropertySnapshotStore store, ILogger<PropertyCreatedHandler> logger)
    : IEventHandler<PropertyCreatedEvent>
{
    public async Task HandleAsync(PropertyCreatedEvent e, CancellationToken ct)
    {
        var applied = await store.ApplyOnceAsync(e.EventId, nameof(PropertyCreatedEvent),
            new SnapshotChange(e.PropertyId, e.OccurredAt, e.Title, e.Price, e.City, e.Status), ct);

        if (applied)
            logger.LogInformation("Property {PropertyId} added to local copy: {Title}, {Price} (CorrelationId {CorrelationId})",
                e.PropertyId, e.Title, e.Price, e.CorrelationId);
    }
}

public sealed class PropertyPriceChangedHandler(PropertySnapshotStore store, ILogger<PropertyPriceChangedHandler> logger)
    : IEventHandler<PropertyPriceChangedEvent>
{
    public async Task HandleAsync(PropertyPriceChangedEvent e, CancellationToken ct)
    {
        var applied = await store.ApplyOnceAsync(e.EventId, nameof(PropertyPriceChangedEvent),
            new SnapshotChange(e.PropertyId, e.OccurredAt, Title: e.Title, Price: e.NewPrice), ct);

        if (applied)
            logger.LogInformation(
                "Property {PropertyId} price in local copy {OldPrice} -> {NewPrice} (CorrelationId {CorrelationId}). Submitted applications keep their price snapshot",
                e.PropertyId, e.OldPrice, e.NewPrice, e.CorrelationId);
    }
}

public sealed class PropertyRenamedHandler(PropertySnapshotStore store, ILogger<PropertyRenamedHandler> logger)
    : IEventHandler<PropertyRenamedEvent>
{
    public async Task HandleAsync(PropertyRenamedEvent e, CancellationToken ct)
    {
        var applied = await store.ApplyOnceAsync(e.EventId, nameof(PropertyRenamedEvent),
            new SnapshotChange(e.PropertyId, e.OccurredAt, Title: e.NewTitle), ct);

        if (applied)
            logger.LogInformation("Property {PropertyId} renamed in local copy: '{OldTitle}' -> '{NewTitle}' (CorrelationId {CorrelationId})",
                e.PropertyId, e.OldTitle, e.NewTitle, e.CorrelationId);
    }
}

public sealed class PropertyDeletedHandler(PropertySnapshotStore store, ILogger<PropertyDeletedHandler> logger)
    : IEventHandler<PropertyDeletedEvent>
{
    public async Task HandleAsync(PropertyDeletedEvent e, CancellationToken ct)
    {
        // м'яке видалення копії: IsDeleted = 1 (історичні заявки посилаються на обʼєкт, тому рядок лишається)
        var applied = await store.ApplyOnceAsync(e.EventId, nameof(PropertyDeletedEvent),
            new SnapshotChange(e.PropertyId, e.OccurredAt, Title: e.Title, IsDeleted: true), ct);

        if (applied)
            logger.LogInformation("Property {PropertyId} marked deleted in local copy (CorrelationId {CorrelationId})",
                e.PropertyId, e.CorrelationId);
    }
}
