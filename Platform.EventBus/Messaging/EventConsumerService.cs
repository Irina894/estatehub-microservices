using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus.Messaging;

/// <summary>
/// Споживач подій як BackgroundService (по одному на чергу сервісу).
///   * топологія оголошується при старті; BasicQos обмежує кількість непідтверджених повідомлень;
///   * autoAck: false - підтвердження вручну:
///       успіх                         -> BasicAck (повідомлення видаляється з черги);
///       пошкоджений JSON / постійний  -> BasicNack(requeue: false) -> Dead Letter Exchange -> DLQ;
///       тимчасовий збій               -> повторна публікація з x-retry-count + 1, оригінал підтверджується;
///       спроби вичерпано              -> BasicNack(requeue: false) -> DLQ;
///   * зупинка застосунку - через stoppingToken: канал закривається, непідтверджені повідомлення повертаються в чергу.
/// </summary>
public sealed class EventConsumerService(
    ConsumerDefinition definition,
    EventHandlerMap handlers,
    IServiceProvider services,
    EventBusMetrics metrics,
    ILogger<EventConsumerService> logger) : BackgroundService
{
    public const int MaxRetries = 3;
    private const int Prefetch = 10;
    private const int MaxSupportedSchemaVersion = 1;
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DlqPollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Цикл на випадок, якщо брокер ще не готовий під час старту: не «кладемо» застосунок, а повторюємо спробу
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Consumer of queue {Queue} failed to start/run; retry in {Delay} s",
                    definition.Queue, ReconnectDelay.TotalSeconds);
            }

            try { await Task.Delay(ReconnectDelay, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        // IConnection беремо з DI тут, а не в конструкторі: якщо брокера ще немає, помилка потрапляє в цикл повторів вище
        var connection = services.GetRequiredService<IConnection>();

        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await EventBusTopology.DeclareConsumerQueueAsync(channel, definition, ct);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: Prefetch, global: false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) => OnMessageAsync(channel, ea, ct);

        await channel.BasicConsumeAsync(definition.Queue, autoAck: false, consumer, ct);
        logger.LogInformation("Consumer started: queue {Queue}, exchange {Exchange}, bindings {Bindings}",
            definition.Queue, definition.Exchange, string.Join(", ", definition.BindingKeys));

        // Поки працюємо - раз на 15 с оновлюємо gauge глибини DLQ
        while (!ct.IsCancellationRequested)
        {
            await UpdateDlqDepthAsync(connection, ct);
            await Task.Delay(DlqPollInterval, ct);
        }
    }

    private async Task OnMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        using var activity = TracePropagation.StartConsumerActivity(ea, definition.Queue);

        var messageId = ea.BasicProperties.MessageId;
        var eventType = ea.BasicProperties.Type;
        var correlationId = ReadHeader(ea.BasicProperties.Headers, "x-correlation-id");

        // Бізнес-контекст у кожному записі логу цього повідомлення (структуровані властивості для фільтрів у Dashboard)
        using var logScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["EventId"] = messageId,
            ["EventType"] = eventType,
            ["CorrelationId"] = correlationId,
            ["Queue"] = definition.Queue
        });

        try
        {
            if (!handlers.TryGet(eventType, out var binding))
            {
                // Цей сервіс не обробляє такий тип (напр. property.created для Reviews): не збій, підтверджуємо
                logger.LogWarning("No handler for event type {EventType} in queue {Queue}; message acknowledged and skipped",
                    eventType, definition.Queue);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
                metrics.Processed(definition.Queue, "ack");
                return;
            }

            var schemaVersion = ReadHeader(ea.BasicProperties.Headers, "x-schema-version");
            if (int.TryParse(schemaVersion, out var version) && version > MaxSupportedSchemaVersion)
                throw new PermanentEventException(
                    $"Unsupported schema version {version} (supported up to {MaxSupportedSchemaVersion})");

            var @event = JsonSerializer.Deserialize(ea.Body.Span, binding.EventType, JsonDefaults.Options)
                         ?? throw new JsonException("Empty message");

            logger.LogInformation("Received {EventType} {EventId} from {Queue} (delivery #{Attempt})",
                eventType, messageId, definition.Queue, GetRetryCount(ea) + 1);

            await using var scope = services.CreateAsyncScope();     // окремий scope на повідомлення
            await binding.Invoke(scope.ServiceProvider, @event, ct);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
            metrics.Processed(definition.Queue, "ack");
            logger.LogInformation("Acked {EventType} {EventId}", eventType, messageId);
        }
        catch (Exception ex) when (ex is JsonException or PermanentEventException)
        {
            // Постійний збій: повтор нічого не змінить. requeue:false -> RabbitMQ перенесе повідомлення в DLX/DLQ
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Permanent failure for {EventType} {EventId}: sending to DLQ {Dlq} (nack, requeue: false)",
                eventType, messageId, definition.DeadLetterQueue);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
            metrics.Processed(definition.Queue, "dead_letter");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Зупинка застосунку: повідомлення не підтверджуємо - брокер поверне його в чергу
            logger.LogInformation("Shutdown while processing {EventId}; message stays in the queue", messageId);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await RetryOrDeadLetterAsync(channel, ea, ex, ct);
        }
    }

    /// <summary>
    /// Тимчасовий збій (БД недоступна, таймаут): повідомлення публікується ПОВТОРНО з x-retry-count + 1, оригінал
    /// підтверджується. (BasicNack(requeue: true) повернув би його без змін - лічильник у заголовку не оновити.)
    /// Повтор іде прямо в чергу цього споживача через default exchange, а НЕ в topic exchange: інакше копію
    /// отримали б усі інші черги, прив'язані до того самого routing key.
    /// </summary>
    private async Task RetryOrDeadLetterAsync(IChannel channel, BasicDeliverEventArgs ea, Exception ex, CancellationToken ct)
    {
        var messageId = ea.BasicProperties.MessageId;
        var attempt = GetRetryCount(ea);

        if (attempt >= MaxRetries)
        {
            logger.LogError(ex, "Message {MessageId} ({EventType}) failed after {Attempts} retries: sending to DLQ {Dlq}",
                messageId, ea.BasicProperties.Type, attempt, definition.DeadLetterQueue);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
            metrics.Processed(definition.Queue, "dead_letter");
            return;
        }

        try
        {
            // невелика експоненційна пауза: 0.5 с, 1 с, 2 с - даємо залежності шанс відновитись
            await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt)), ct);

            var headers = new Dictionary<string, object?>(ea.BasicProperties.Headers ?? new Dictionary<string, object?>())
            {
                ["x-retry-count"] = attempt + 1
            };
            var props = new BasicProperties(ea.BasicProperties) { Headers = headers };

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: definition.Queue,
                mandatory: false, props, ea.Body, ct);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);

            metrics.Processed(definition.Queue, "retry");
            logger.LogWarning(ex, "Message {MessageId} ({EventType}) failed, retry {Attempt}/{Max}: {Reason}",
                messageId, ea.BasicProperties.Type, attempt + 1, MaxRetries, ex.Message);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // зупинка: оригінал лишається непідтвердженим і повернеться в чергу
        }
        catch (Exception publishEx)
        {
            // не вдалось навіть повторно опублікувати: повертаємо оригінал у чергу, щоб нічого не втратити
            logger.LogError(publishEx, "Could not republish {MessageId} for retry; requeue", messageId);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
        }
    }

    private async Task UpdateDlqDepthAsync(IConnection connection, CancellationToken ct)
    {
        try
        {
            // passive declare на помилку закриває канал, тому для цього використовуємо окремий короткоживучий
            await using var ch = await connection.CreateChannelAsync(cancellationToken: ct);
            var ok = await ch.QueueDeclarePassiveAsync(definition.DeadLetterQueue, ct);
            metrics.SetDlqDepth(definition.DeadLetterQueue, ok.MessageCount);

            if (ok.MessageCount > 0)
                logger.LogWarning("DLQ {Dlq} contains {Count} message(s): manual investigation required",
                    definition.DeadLetterQueue, ok.MessageCount);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not read depth of {Dlq}", definition.DeadLetterQueue);
        }
    }

    private static int GetRetryCount(BasicDeliverEventArgs ea) =>
        int.TryParse(ReadHeader(ea.BasicProperties.Headers, "x-retry-count"), out var n) ? n : 0;

    /// <summary>Значення заголовків від брокера можуть бути byte[] (рядки) або числами.</summary>
    private static string? ReadHeader(IDictionary<string, object?>? headers, string key) =>
        headers is not null && headers.TryGetValue(key, out var v)
            ? v switch { byte[] b => Encoding.UTF8.GetString(b), null => null, _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) }
            : null;
}
