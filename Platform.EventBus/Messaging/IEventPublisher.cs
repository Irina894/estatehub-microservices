using Platform.EventBus.Events;

namespace Platform.EventBus.Messaging;

public interface IEventPublisher
{
    /// <summary>Серіалізує подію в JSON і публікує її в exchange з routing key (persistent-доставка).</summary>
    Task PublishAsync<T>(T @event, string exchange, string routingKey, CancellationToken ct = default)
        where T : IntegrationEvent;
}
