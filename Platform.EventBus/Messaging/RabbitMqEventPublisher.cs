using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using RabbitMQ.Client;

namespace Platform.EventBus.Messaging;

/// <summary>
/// Публікація подій у RabbitMQ. IConnection - один на застосунок (singleton, реєструє Aspire.RabbitMQ.Client),
/// канал (IChannel) створюється на кожну публікацію й одразу закривається - канали недорогі, але не потокобезпечні.
/// Publisher нічого не знає про споживачів: він лише кладе подію в exchange.
/// </summary>
public sealed class RabbitMqEventPublisher(
    IConnection connection, EventBusMetrics metrics, ILogger<RabbitMqEventPublisher> logger) : IEventPublisher
{
    private readonly SemaphoreSlim _topologyLock = new(1, 1);
    private volatile bool _topologyReady;

    public async Task PublishAsync<T>(T @event, string exchange, string routingKey, CancellationToken ct = default)
        where T : IntegrationEvent
    {
        // Producer-span: від нього починається гілка trace, яку consumer продовжить (TracePropagation)
        using var activity = TracePropagation.Source.StartActivity($"{exchange} publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", exchange);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", routingKey);
        activity?.SetTag("messaging.message.id", @event.EventId.ToString());

        var eventType = @event.GetType().Name;

        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await EnsureTopologyAsync(channel, exchange, ct);

        var props = new BasicProperties
        {
            Persistent = true,                                   // повідомлення зберігається на диску (delivery-mode 2)
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),
            Type = eventType,
            Timestamp = new AmqpTimestamp(new DateTimeOffset(@event.OccurredAt, TimeSpan.Zero).ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?> { ["x-schema-version"] = @event.SchemaVersion }
        };
        if (!string.IsNullOrEmpty(@event.CorrelationId))
            props.Headers["x-correlation-id"] = @event.CorrelationId;
        TracePropagation.Inject(props);                          // traceparent у заголовки

        // GetType(), а не typeof(T): серіалізуємо всі поля фактичного типу, навіть якщо T - базовий тип
        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), JsonDefaults.Options);

        await channel.BasicPublishAsync(exchange, routingKey, mandatory: false, props, body, ct);

        metrics.Published(eventType);
        logger.LogInformation(
            "Published {EventType} {EventId} (CorrelationId {CorrelationId}) to {Exchange}/{RoutingKey}",
            eventType, @event.EventId, @event.CorrelationId, exchange, routingKey);
    }

    /// <summary>
    /// Гарантує, що exchange і черги відомих споживачів існують ще ДО першої публікації: інакше подія, опублікована,
    /// коли споживач ще жодного разу не стартував, нікуди б не потрапила (exchange без bindings відкидає повідомлення).
    /// </summary>
    private async Task EnsureTopologyAsync(IChannel channel, string exchange, CancellationToken ct)
    {
        if (_topologyReady) return;

        await _topologyLock.WaitAsync(ct);
        try
        {
            if (_topologyReady) return;

            await EventBusTopology.DeclareExchangeAsync(channel, exchange, ct);
            foreach (var consumer in EventBusDefinitions.AllCatalogConsumers.Where(c => c.Exchange == exchange))
                await EventBusTopology.DeclareConsumerQueueAsync(channel, consumer, ct);

            _topologyReady = true;
            logger.LogInformation("Topology for exchange {Exchange} declared", exchange);
        }
        finally
        {
            _topologyLock.Release();
        }
    }
}
