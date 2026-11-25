using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Messaging;

namespace Platform.EventBus;

public static class EventBusExtensions
{
    /// <summary>
    /// Підключення до RabbitMQ через Aspire (рядок підключення "messaging" передає AppHost) + publisher + метрики.
    /// AddRabbitMQClient реєструє IConnection (singleton), health check і телеметрію.
    /// </summary>
    public static TBuilder AddEventBus<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.AddRabbitMQClient("messaging");
        builder.Services.AddSingleton<EventBusMetrics>();
        builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        return builder;
    }

    /// <summary>Реєструє споживача черги як hosted service і його обробники (Scoped).</summary>
    public static IServiceCollection AddEventConsumer(
        this IServiceCollection services, ConsumerDefinition definition, Action<EventHandlerMap> configureHandlers)
    {
        var map = new EventHandlerMap();
        configureHandlers(map);

        foreach (var handlerType in map.HandlerTypes)
            services.AddScoped(handlerType);

        services.AddHostedService(sp => new EventConsumerService(
            definition, map, sp,
            sp.GetRequiredService<EventBusMetrics>(),
            sp.GetRequiredService<ILogger<EventConsumerService>>()));

        return services;
    }
}
