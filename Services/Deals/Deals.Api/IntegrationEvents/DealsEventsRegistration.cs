using Platform.EventBus;
using Platform.EventBus.Events;
using Platform.EventBus.Messaging;

namespace Deals.Api.IntegrationEvents;

public static class DealsEventsRegistration
{
    /// <summary>
    /// RabbitMQ (Лаб. 8): IConnection + health check (Aspire), publisher і споживач черги deals.catalog-sync
    /// (BackgroundService з ручним ack), який синхронізує локальну копію Properties.
    /// </summary>
    public static IHostApplicationBuilder AddDealsEvents(this IHostApplicationBuilder builder)
    {
        builder.AddEventBus();
        builder.Services.AddSingleton<PropertySnapshotStore>();
        builder.Services.AddEventConsumer(EventBusDefinitions.DealsCatalogSync, map => map
            .Handle<PropertyCreatedEvent, PropertyCreatedHandler>()
            .Handle<PropertyPriceChangedEvent, PropertyPriceChangedHandler>()
            .Handle<PropertyRenamedEvent, PropertyRenamedHandler>()
            .Handle<PropertyDeletedEvent, PropertyDeletedHandler>());
        return builder;
    }
}
