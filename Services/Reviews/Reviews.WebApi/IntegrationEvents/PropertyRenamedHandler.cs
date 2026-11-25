using MongoDB.Bson;
using MongoDB.Driver;
using Platform.EventBus.Events;
using Platform.EventBus.Messaging;
using Reviews.Application.Caching;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Persistence;

namespace Reviews.WebApi.IntegrationEvents;

/// <summary>
/// Реплікація даних: коли Каталог перейменовує обʼєкт, оновлюємо денормалізоване поле productName у всіх відгуках
/// про нього (read model для списків відгуків).
///
/// Ідемпотентність (колекція processedEvents, _id = EventId): UpdateMany ... Set(ProductName) за своєю природою
/// ідемпотентний, тож порядок «перевірка -> зміна -> позначка» безпечний: якщо застосунок впаде між зміною й позначкою,
/// повторна доставка просто повторить ту саму зміну. Окрема транзакція «зміна + позначка» тут неможлива: MongoDB
/// у контейнері Aspire працює без replica set, а транзакції потребують саме його.
///
/// Out-of-order: колекція propertyNameVersions тримає час останньої застосованої перейменування обʼєкта;
/// запізніла (старіша) подія не перезаписує свіжішу назву.
/// Відгуки зіставляються за OldTitle, бо в Reviews обʼєкт ідентифікується числом, а в Каталозі - Guid
/// (така сама тимчасова відповідність за назвою використовується в Aggregator).
/// </summary>
public sealed class PropertyRenamedHandler(
    MongoDbContext db, ITwoLevelCache cache, ILogger<PropertyRenamedHandler> logger)
    : IEventHandler<PropertyRenamedEvent>
{
    private const string ProcessedEventsCollection = "processedEvents";
    private const string NameVersionsCollection = "propertyNameVersions";

    public async Task HandleAsync(PropertyRenamedEvent e, CancellationToken ct)
    {
        var processed = db.Database.GetCollection<BsonDocument>(ProcessedEventsCollection);
        var versions = db.Database.GetCollection<BsonDocument>(NameVersionsCollection);
        var eventId = e.EventId.ToString();

        // 1) ідемпотентність
        if (await processed.Find(Builders<BsonDocument>.Filter.Eq("_id", eventId)).AnyAsync(ct))
        {
            logger.LogInformation("Duplicate event {EventId} skipped (already processed)", e.EventId);
            return;
        }

        // 2) out-of-order: чи не застосовували вже новішу зміну назви цього обʼєкта
        var at = DateTime.SpecifyKind(e.OccurredAt, DateTimeKind.Utc);
        var newer = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("_id", e.PropertyId.ToString()),
            Builders<BsonDocument>.Filter.Gte("lastEventAt", at));

        if (await versions.Find(newer).AnyAsync(ct))
        {
            logger.LogWarning(
                "Stale PropertyRenamedEvent {EventId} for property {PropertyId} ignored: a newer rename was already applied",
                e.EventId, e.PropertyId);
        }
        else
        {
            // 3) бізнес-дія: назва в усіх відгуках про обʼєкт
            var result = await db.Reviews.UpdateManyAsync(
                r => r.ProductName == e.OldTitle,
                Builders<Review>.Update.Set(r => r.ProductName, e.NewTitle),
                cancellationToken: ct);

            await versions.ReplaceOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", e.PropertyId.ToString()),
                new BsonDocument { { "_id", e.PropertyId.ToString() }, { "title", e.NewTitle }, { "lastEventAt", at } },
                new ReplaceOptions { IsUpsert = true }, ct);

            // кеш списків відгуків містить стару назву: змінюємо версію групи, і всі списки перечитаються з MongoDB
            await cache.BumpVersionAsync(ReviewsCacheKeys.ListScope, $"property {e.PropertyId} renamed", ct);

            logger.LogInformation(
                "productName updated in {Count} review(s): '{OldTitle}' -> '{NewTitle}' (PropertyId {PropertyId}, CorrelationId {CorrelationId})",
                result.ModifiedCount, e.OldTitle, e.NewTitle, e.PropertyId, e.CorrelationId);
        }

        // 4) позначка «оброблено» (дублікат ключа при гонці двох екземплярів - не помилка)
        try
        {
            await processed.InsertOneAsync(
                new BsonDocument { { "_id", eventId }, { "processedAt", DateTime.UtcNow } }, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            logger.LogInformation("Event {EventId} was marked as processed concurrently", e.EventId);
        }
    }
}
