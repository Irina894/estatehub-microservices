using Catalog.Bll.Dtos;
using Platform.EventBus.Events;
using Platform.EventBus.Messaging;
using Platform.ServiceDefaults;

namespace Catalog.Api.IntegrationEvents;

/// <summary>
/// Публікація подій Каталогу. Викликається контролером ПІСЛЯ того, як сервіс успішно зберіг зміни в БД
/// (SaveChanges всередині service.*Async): якщо збереження кинуло виняток, до цього коду виконання не доходить,
/// тобто подія не публікується. Подія містить усі дані для споживачів (самодостатня), CorrelationId бере з HTTP-запиту.
///
/// Обмеження: між збереженням і публікацією застосунок може впасти - тоді зміна збережена, а подію втрачено.
/// Повну гарантію дає Transactional Outbox (за бажанням у завданні); тут збій публікації лише логується як помилка,
/// щоб клієнт не отримав 500 після вже виконаної зміни.
/// </summary>
public sealed class CatalogEventPublisher(
    IEventPublisher publisher, IHttpContextAccessor httpContext, ILogger<CatalogEventPublisher> logger)
{
    private string? CorrelationId =>
        httpContext.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString() is { Length: > 0 } id ? id : null;

    public Task PropertyCreatedAsync(PropertyDto created) =>
        SafePublishAsync(
            new PropertyCreatedEvent(created.Id, created.Title, created.Price, created.City, created.Status)
            { CorrelationId = CorrelationId },
            CatalogEventRoutes.PropertyCreated, created.Id);

    public Task PriceChangedAsync(Guid propertyId, string title, decimal oldPrice, decimal newPrice) =>
        oldPrice == newPrice
            ? Task.CompletedTask   // ціна не змінилась - події немає
            : SafePublishAsync(
                new PropertyPriceChangedEvent(propertyId, title, oldPrice, newPrice) { CorrelationId = CorrelationId },
                CatalogEventRoutes.PropertyPriceChanged, propertyId);

    public Task RenamedAsync(Guid propertyId, string oldTitle, string newTitle) =>
        string.Equals(oldTitle, newTitle, StringComparison.Ordinal)
            ? Task.CompletedTask
            : SafePublishAsync(
                new PropertyRenamedEvent(propertyId, oldTitle, newTitle) { CorrelationId = CorrelationId },
                CatalogEventRoutes.PropertyRenamed, propertyId);

    public Task DeletedAsync(Guid propertyId, string title) =>
        SafePublishAsync(
            new PropertyDeletedEvent(propertyId, title) { CorrelationId = CorrelationId },
            CatalogEventRoutes.PropertyDeleted, propertyId);

    private async Task SafePublishAsync<T>(T @event, string routingKey, Guid propertyId) where T : IntegrationEvent
    {
        try
        {
            // CancellationToken.None: клієнт міг розірвати з'єднання, але зміна в БД уже збережена - подію все одно треба відправити
            await publisher.PublishAsync(@event, CatalogEventRoutes.Exchange, routingKey, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish {EventType} {EventId} for property {PropertyId} (CorrelationId {CorrelationId}): " +
                "the change is saved but consumers will not be notified (consider Transactional Outbox)",
                typeof(T).Name, @event.EventId, propertyId, @event.CorrelationId);
        }
    }
}
