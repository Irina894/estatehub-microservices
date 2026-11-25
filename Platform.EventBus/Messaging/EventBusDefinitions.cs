using Platform.EventBus.Events;

namespace Platform.EventBus.Messaging;

/// <summary>
/// Черги сервісів-споживачів подій Каталогу. Назви описові: &lt;споживач&gt;.&lt;джерело&gt;-sync.
/// Їх знають і publisher (щоб гарантувати існування черг ще до першої публікації), і самі споживачі.
/// </summary>
public static class EventBusDefinitions
{
    /// <summary>Deals: локальна копія довідника Properties (створення, ціна, назва, видалення) - усі події property.*.</summary>
    public static readonly ConsumerDefinition DealsCatalogSync =
        new("deals.catalog-sync", CatalogEventRoutes.Exchange, ["property.*"]);

    /// <summary>Reviews: потрібна лише перейменована назва для денормалізованого поля productName.</summary>
    public static readonly ConsumerDefinition ReviewsCatalogSync =
        new("reviews.catalog-sync", CatalogEventRoutes.Exchange, [CatalogEventRoutes.PropertyRenamed]);

    public static readonly IReadOnlyList<ConsumerDefinition> AllCatalogConsumers = [DealsCatalogSync, ReviewsCatalogSync];
}
