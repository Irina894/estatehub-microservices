using Platform.EventBus;
using Platform.EventBus.Events;
using Platform.EventBus.Messaging;

namespace Reviews.WebApi.IntegrationEvents;

public static class ReviewsEventsRegistration
{
    /// <summary>RabbitMQ (Лаб. 8): IConnection + health check (Aspire) і споживач черги reviews.catalog-sync.</summary>
    public static IHostApplicationBuilder AddReviewsEvents(this IHostApplicationBuilder builder)
    {
        builder.AddEventBus();
        builder.Services.AddEventConsumer(EventBusDefinitions.ReviewsCatalogSync, map => map
            .Handle<PropertyRenamedEvent, PropertyRenamedHandler>());
        return builder;
    }
}
