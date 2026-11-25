namespace Platform.EventBus.Events;

/// <summary>Імена exchange і routing key подій Каталогу (єдине місце, спільне для publisher і consumers).</summary>
public static class CatalogEventRoutes
{
    public const string Exchange = "catalog-events";

    public const string PropertyCreated = "property.created";
    public const string PropertyPriceChanged = "property.price_changed";
    public const string PropertyRenamed = "property.renamed";
    public const string PropertyDeleted = "property.deleted";
}

// Усі події самодостатні: споживач не звертається до Каталогу по додаткові дані.
// Обʼєкт у домені - Property (в еталонному eShop цей аналог називається Product).

/// <summary>Створено обʼєкт: повний знімок даних, потрібних для копій в інших сервісах.</summary>
public sealed record PropertyCreatedEvent(Guid PropertyId, string Title, decimal Price, string City, string Status)
    : IntegrationEvent;

/// <summary>Змінилась ціна. Title - актуальна назва на цей момент (щоб споживач міг створити/виправити копію).</summary>
public sealed record PropertyPriceChangedEvent(Guid PropertyId, string Title, decimal OldPrice, decimal NewPrice)
    : IntegrationEvent;

/// <summary>Змінилась назва. OldTitle потрібен сервісу Reviews: там обʼєкт ідентифікується числом, а не Guid.</summary>
public sealed record PropertyRenamedEvent(Guid PropertyId, string OldTitle, string NewTitle)
    : IntegrationEvent;

/// <summary>Обʼєкт видалено (м'яке видалення в Каталозі).</summary>
public sealed record PropertyDeletedEvent(Guid PropertyId, string Title)
    : IntegrationEvent;
