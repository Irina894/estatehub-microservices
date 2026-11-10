using System.Diagnostics;
using Microsoft.Extensions.Options;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Services;

/// <summary>
/// HTTP-версія Aggregator (Лаб. 5), збережена для порівняння з gRPC (?transport=http). Збирає картку заявки з трьох сервісів. Заявка обов'язкова (без неї агрегувати нічого),
/// а об'єкт і відгуки запитуються ПАРАЛЕЛЬНО (Task.WhenAll); якщо один недоступний - відповідь часткова.
/// </summary>
public class HttpApplicationAggregationService(
    DealsClient deals,
    CatalogClient catalog,
    ReviewsClient reviews,
    IOptions<AggregatorOptions> options,
    ILogger<HttpApplicationAggregationService> logger)
    : IApplicationAggregator
{
    private static readonly ActivitySource Source = new("EstateHub.Aggregator");

    private readonly AggregatorOptions _options = options.Value;

    public async Task<FullApplicationDto?> GetFullApplicationAsync(Guid applicationId, CancellationToken ct)
    {
        using var activity = Source.StartActivity("AggregateApplication.Http");
        activity?.SetTag("aggregator.transport", "http");
        activity?.SetTag("application.id", applicationId);

        var warnings = new List<string>();

        logger.LogInformation("Calling deals-api for application {ApplicationId}", applicationId);
        var application = await deals.GetApplicationAsync(applicationId, ct);   // якщо deals-api недоступний - це помилка
        if (application is null) return null;

        var productId = ResolveReviewProductId(application.PropertyTitleSnapshot);

        // незалежні виклики стартують одночасно
        var propertyTask = SafeAsync(
            () => catalog.GetPropertyAsync(application.PropertyId, ct),
            "catalog-api", $"property {application.PropertyId}", warnings, ct);

        var reviewsTask = productId is null
            ? Task.FromResult<IReadOnlyList<ReviewDto>?>(null)
            : SafeAsync<IReadOnlyList<ReviewDto>>(
                async () => await reviews.GetForProductAsync(productId.Value, ct),
                "reviews-api", $"product {productId}", warnings, ct);

        await Task.WhenAll(propertyTask, reviewsTask);

        var property = await propertyTask;
        var reviewItems = await reviewsTask ?? [];

        if (property is null && !warnings.Any(w => w.StartsWith("catalog-api", StringComparison.Ordinal)))
            warnings.Add($"catalog-api: property {application.PropertyId} not found");
        if (productId is null)
            warnings.Add($"reviews-api: no review mapping for '{application.PropertyTitleSnapshot}'");

        // перевірка узгодженості: відповідь стосується саме цієї заявки
        if (property is not null && property.Id != application.PropertyId)
            warnings.Add("catalog-api returned a property that does not match the application");

        double? average = reviewItems.Count == 0 ? null : Math.Round(reviewItems.Average(r => r.Rating), 2);

        return new FullApplicationDto(application, property, reviewItems, average, warnings);
    }

    private long? ResolveReviewProductId(string title)
    {
        var normalized = title.Trim();
        var map = _options.ReviewProductMap
            .FirstOrDefault(m => string.Equals(m.Title, normalized, StringComparison.OrdinalIgnoreCase));
        return map?.ProductId;
    }

    /// <summary>
    /// Виклик одного сервісу з логуванням, таймаутом і перетворенням збою на попередження.
    /// Скасування клієнтом (ct) НЕ ковтається - воно припиняє весь запит.
    /// </summary>
    private async Task<T?> SafeAsync<T>(
        Func<Task<T?>> call, string service, string what, List<string> warnings, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Calling {Service} for {What}", service, what);
        try
        {
            var result = await call().WaitAsync(TimeSpan.FromSeconds(_options.ServiceTimeoutSeconds), ct);
            logger.LogInformation("{Service} responded in {Elapsed} ms", service, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "{Service} is unavailable after {Elapsed} ms", service, sw.ElapsedMilliseconds);
            lock (warnings) warnings.Add($"{service} is unavailable");
            return default;
        }
    }
}
