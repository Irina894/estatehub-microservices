using Platform.EventBus.Events;

namespace Platform.EventBus.Messaging;

/// <summary>Обробник одного типу події (окремий клас на тип; залежності - через DI у межах scope повідомлення).</summary>
public interface IEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}

/// <summary>
/// Постійний збій обробки (наприклад, помилка валідації бізнес-даних): повтор не допоможе,
/// тому повідомлення одразу йде в DLQ без retry.
/// </summary>
public sealed class PermanentEventException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Реєстр «тип події (BasicProperties.Type) → обробник» для одного споживача.</summary>
public sealed class EventHandlerMap
{
    internal sealed record Binding(Type EventType, Type HandlerType, Func<IServiceProvider, object, CancellationToken, Task> Invoke);

    private readonly Dictionary<string, Binding> _bindings = new(StringComparer.Ordinal);

    internal IEnumerable<Type> HandlerTypes => _bindings.Values.Select(b => b.HandlerType).Distinct();

    public EventHandlerMap Handle<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IEventHandler<TEvent>
    {
        _bindings[typeof(TEvent).Name] = new Binding(
            typeof(TEvent), typeof(THandler),
            (sp, evt, ct) => ((THandler)sp.GetService(typeof(THandler))!).HandleAsync((TEvent)evt, ct));
        return this;
    }

    internal bool TryGet(string? eventType, out Binding binding)
    {
        if (eventType is not null && _bindings.TryGetValue(eventType, out var b))
        {
            binding = b;
            return true;
        }

        binding = null!;
        return false;
    }
}
