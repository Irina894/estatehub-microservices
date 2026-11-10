using System.Diagnostics;
using Catalog.Grpc;
using Deals.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Platform.Aggregator.Models;
using Reviews.Grpc;

namespace Platform.Aggregator.Services;

/// <summary>
/// Aggregator на gRPC (Лаб. 6). Та сама логіка, що й у HTTP-версії, але виклики йдуть типізованими gRPC-клієнтами,
/// згенерованими з .proto, через одне мультиплексоване HTTP/2-з'єднання на сервіс.
/// Заявка обов'язкова; об'єкт і відгуки запитуються ПАРАЛЕЛЬНО (Task.WhenAll); збій одного з них дає часткову відповідь.
/// </summary>
public class GrpcApplicationAggregationService(
    DealsService.DealsServiceClient deals,
    CatalogService.CatalogServiceClient catalog,
    ReviewsService.ReviewsServiceClient reviews,
    IOptions<AggregatorOptions> options,
    ILogger<GrpcApplicationAggregationService> logger) : IApplicationAggregator
{
    private static readonly ActivitySource Source = new("EstateHub.Aggregator");

    private readonly AggregatorOptions _options = options.Value;

    public async Task<FullApplicationDto?> GetFullApplicationAsync(Guid applicationId, CancellationToken ct)
    {
        using var activity = Source.StartActivity("AggregateApplication.Grpc");
        activity?.SetTag("aggregator.transport", "grpc");
        activity?.SetTag("application.id", applicationId);

        var warnings = new List<string>();

        // ---- 1. Заявка (обов'язкова): без неї агрегувати нічого. NotFound -> null (404); недоступність -> виняток ----
        logger.LogInformation("Calling deals-api (gRPC) for application {ApplicationId}", applicationId);
        ApplicationReply reply;
        try
        {
            reply = await deals.GetApplicationAsync(
                new GetApplicationRequest { Id = applicationId.ToString() }, deadline: Deadline(), cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }

        var application = new ApplicationDto(
            Guid.Parse(reply.Id), Guid.Parse(reply.ClientId), Guid.Parse(reply.PropertyId),
            (decimal)reply.PriceSnapshot, reply.PropertyTitleSnapshot, reply.Status,
            reply.CreatedAt.ToDateTime(), null);

        var productId = ResolveReviewProductId(application.PropertyTitleSnapshot);

        // ---- 2. Незалежні виклики стартують одночасно ----
        var propertyTask = SafeAsync(
            async () =>
            {
                try
                {
                    var r = await catalog.GetPropertyAsync(
                        new GetPropertyRequest { Id = application.PropertyId.ToString() }, deadline: Deadline(), cancellationToken: ct);
                    return ToProperty(r);
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
                {
                    return null;   // не збій сервісу, а відсутні дані: попередження додаємо нижче
                }
            },
            "catalog-api", $"property {application.PropertyId}", warnings, ct);

        var reviewsTask = productId is null
            ? Task.FromResult<IReadOnlyList<ReviewDto>?>(null)
            : SafeAsync<IReadOnlyList<ReviewDto>>(
                async () =>
                {
                    var r = await reviews.GetReviewsByProductAsync(
                        new GetReviewsByProductRequest { ProductId = productId.Value, Page = 1, PageSize = 50 },
                        deadline: Deadline(), cancellationToken: ct);
                    return r.Reviews.Select(ToReview).ToList();
                },
                "reviews-api", $"product {productId}", warnings, ct);

        await Task.WhenAll(propertyTask, reviewsTask);

        var property = await propertyTask;
        var reviewItems = await reviewsTask ?? [];

        if (property is null && !warnings.Any(w => w.StartsWith("catalog-api", StringComparison.Ordinal)))
            warnings.Add($"catalog-api: property {application.PropertyId} not found");
        if (productId is null)
            warnings.Add($"reviews-api: no review mapping for '{application.PropertyTitleSnapshot}'");

        double? average = reviewItems.Count == 0 ? null : Math.Round(reviewItems.Average(r => r.Rating), 2);

        return new FullApplicationDto(application, property, reviewItems, average, warnings);
    }

    /// <summary>Дедлайн gRPC-виклику: після нього клієнт отримує DeadlineExceeded, і сервіс вважається недоступним.</summary>
    private DateTime Deadline() => DateTime.UtcNow.AddSeconds(_options.ServiceTimeoutSeconds);

    private long? ResolveReviewProductId(string title)
    {
        var normalized = title.Trim();
        return _options.ReviewProductMap
            .FirstOrDefault(m => string.Equals(m.Title, normalized, StringComparison.OrdinalIgnoreCase))?.ProductId;
    }

    private static PropertyDto ToProperty(PropertyReply p) => new(
        Guid.Parse(p.Id), p.Title, p.Description, (decimal)p.Price, p.City, p.Status, p.CategoryName,
        p.Location is null
            ? null
            : new LocationDto(
                p.Location.FullAddress,
                p.Location.HasLatitude ? p.Location.Latitude : null,
                p.Location.HasLongitude ? p.Location.Longitude : null));

    private static ReviewDto ToReview(ReviewReply r) => new(
        r.Id, r.ProductId, r.ProductName, r.AuthorName, r.Rating, r.Text, r.CommentsCount, r.CreatedAt.ToDateTime());

    /// <summary>
    /// Один gRPC-виклик з логуванням і перетворенням збою на попередження. Скасування клієнтом (ct) не ковтається.
    /// RpcException: Unavailable / DeadlineExceeded = сервіс недоступний; інші статуси лишаються в тексті попередження.
    /// </summary>
    private async Task<T?> SafeAsync<T>(
        Func<Task<T?>> call, string service, string what, List<string> warnings, CancellationToken ct) where T : class
    {
        var started = Stopwatch.GetTimestamp();
        logger.LogInformation("Calling {Service} (gRPC) for {What}", service, what);
        try
        {
            var result = await call();
            logger.LogInformation("{Service} responded in {Elapsed} ms", service, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return result;
        }
        catch (RpcException ex) when (!ct.IsCancellationRequested)
        {
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            logger.LogWarning(ex, "{Service} failed with {Status} after {Elapsed} ms", service, ex.StatusCode, elapsed);
            lock (warnings)
                warnings.Add(ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded
                    ? $"{service} is unavailable"
                    : $"{service} returned {ex.StatusCode}");
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException && !ct.IsCancellationRequested)
        {
            // не-gRPC збої (наприклад, service discovery або мережа)
            logger.LogWarning(ex, "{Service} is unavailable after {Elapsed} ms", service, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            lock (warnings) warnings.Add($"{service} is unavailable");
            return null;
        }
    }
}
