using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using Platform.ServiceDefaults;
using Reviews.Infrastructure.Persistence;

namespace Reviews.WebApi.HealthChecks;

/// <summary>
/// Лаб. 7: readiness-перевірка MongoDB - команда { ping: 1 } з таймаутом 3 с.
/// MongoDB - основне сховище Reviews, тому збій = Unhealthy.
/// </summary>
public sealed class MongoDbHealthCheck(MongoDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthContext, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            await context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cts.Token);
            return HealthCheckResult.Healthy("MongoDB доступна");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("MongoDB недоступна", ex);
        }
    }
}

public static class ReviewsHealthCheckExtensions
{
    public static IHealthChecksBuilder AddReviewsHealthChecks(this IHealthChecksBuilder builder) =>
        builder
            .AddCheck<MongoDbHealthCheck>("mongodb", HealthStatus.Unhealthy, [HealthTags.Ready], TimeSpan.FromSeconds(5))
            .AddRedisCacheHealthCheck();
}
