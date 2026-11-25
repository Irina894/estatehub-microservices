using RabbitMQ.Client;

namespace Platform.EventBus.Messaging;

/// <summary>Опис черги сервісу-споживача: з якого exchange, за якими routing keys.</summary>
public sealed record ConsumerDefinition(string Queue, string Exchange, IReadOnlyList<string> BindingKeys)
{
    public string DeadLetterQueue => $"{Queue}.dlq";
}

/// <summary>
/// Декларативна топологія: виконується при старті кожного споживача/публікатора. Повторне оголошення
/// з тими самими параметрами безпечне; параметри вже створених черг вручну в Management UI не міняємо.
///
///   catalog-events (topic) --product.*--> deals.catalog-sync   --(reject/nack)--> catalog-events.dlx (direct)
///                          --...-------> reviews.catalog-sync                      |-- key=deals.catalog-sync   -> deals.catalog-sync.dlq
///                                                                                  |-- key=reviews.catalog-sync -> reviews.catalog-sync.dlq
/// DLX має тип direct, а ключ = назва вихідної черги: так повідомлення з однієї черги потрапляє лише у свою DLQ
/// (з fanout воно б дублювалось в усі DLQ).
/// </summary>
public static class EventBusTopology
{
    public static string DeadLetterExchange(string exchange) => $"{exchange}.dlx";

    /// <summary>Лише exchange (потрібно publisher-у, щоб подія не пропала, якщо споживачі ще не стартували).</summary>
    public static async Task DeclareExchangeAsync(IChannel ch, string exchange, CancellationToken ct)
    {
        await ch.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
        await ch.ExchangeDeclareAsync(DeadLetterExchange(exchange), ExchangeType.Direct, durable: true, autoDelete: false,
            cancellationToken: ct);
    }

    public static async Task DeclareConsumerQueueAsync(IChannel ch, ConsumerDefinition def, CancellationToken ct)
    {
        await DeclareExchangeAsync(ch, def.Exchange, ct);
        var dlx = DeadLetterExchange(def.Exchange);

        // основна черга: durable + dead-letter налаштування
        await ch.QueueDeclareAsync(def.Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = dlx,
                ["x-dead-letter-routing-key"] = def.Queue
            },
            cancellationToken: ct);

        foreach (var key in def.BindingKeys)
            await ch.QueueBindAsync(def.Queue, def.Exchange, key, cancellationToken: ct);

        // DLQ прив'язана до DLX за ключем = назва основної черги
        await ch.QueueDeclareAsync(def.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: ct);
        await ch.QueueBindAsync(def.DeadLetterQueue, dlx, def.Queue, cancellationToken: ct);
    }
}
