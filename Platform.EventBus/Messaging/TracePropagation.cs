using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus.Messaging;

/// <summary>
/// Передача контексту трасування через брокер (W3C traceparent у заголовках повідомлення):
/// publisher записує, consumer відновлює - тож у Dashboard один trace: HTTP → Publish → Consume → DB.
/// </summary>
public static class TracePropagation
{
    /// <summary>Назва ActivitySource; додана до AddSource у ServiceDefaults.</summary>
    public const string SourceName = "Platform.Messaging";

    public static readonly ActivitySource Source = new(SourceName);

    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    /// <summary>Записує traceparent (і baggage) поточного Activity в заголовки повідомлення.</summary>
    public static void Inject(BasicProperties props)
    {
        if (Activity.Current is not { } activity) return;

        props.Headers ??= new Dictionary<string, object?>();
        Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), props.Headers,
            (headers, key, value) => headers[key] = value);
    }

    /// <summary>Відновлює контекст із заголовків (значення приходять як byte[]) і стартує Consumer-activity.</summary>
    public static Activity? StartConsumerActivity(BasicDeliverEventArgs ea, string queue)
    {
        var parent = Propagator.Extract(default, ea.BasicProperties.Headers,
            (headers, key) => headers is not null && headers.TryGetValue(key, out var v) && v is byte[] bytes
                ? [Encoding.UTF8.GetString(bytes)]
                : []);

        Baggage.Current = parent.Baggage;

        var activity = Source.StartActivity($"{queue} process", ActivityKind.Consumer, parent.ActivityContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", queue);
        activity?.SetTag("messaging.message.id", ea.BasicProperties.MessageId);
        activity?.SetTag("messaging.message.type", ea.BasicProperties.Type);
        activity?.SetTag("messaging.operation", "process");
        return activity;
    }
}
