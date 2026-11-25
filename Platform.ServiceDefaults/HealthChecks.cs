using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;

namespace Platform.ServiceDefaults;

/// <summary>Теги health checks: live - лише процес, ready - процес + залежності.</summary>
public static class HealthTags
{
    public const string Live = "live";
    public const string Ready = "ready";
}

/// <summary>
/// Лаб. 7: перевірка Redis (L2-кеш). Redis - ОПЦІЙНА залежність (Лаб. 6: TwoLevelCache деградує до L1),
/// тому збій дає Degraded, а не Unhealthy: сервіс лишається Ready, але повільніший.
/// Перевірка йде через IDistributedCache, тож не потрібен додатковий пакет.
/// </summary>
public sealed class RedisCacheHealthCheck(IServiceProvider services) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var cache = services.GetService<IDistributedCache>();
        if (cache is null)
            return HealthCheckResult.Healthy("Distributed cache не налаштовано");

        try
        {
            // читання неіснуючого ключа: перевіряє з'єднання, нічого не пише
            await cache.GetAsync("health:ping", ct).WaitAsync(Timeout, ct);
            return HealthCheckResult.Healthy("Redis доступний");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return HealthCheckResult.Degraded(
                "Redis недоступний - працюємо лише з L1 (in-memory) кешем", ex);
        }
    }
}

/// <summary>JSON-відповідь health check: загальний статус, час, і для кожної перевірки статус/опис/тривалість.</summary>
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                error = e.Value.Exception?.Message,
                tags = e.Value.Tags
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, Json));
    }
}

public static class HealthCheckExtensions
{
    /// <summary>Реєструє перевірку Redis з тегом ready (Degraded при збої).</summary>
    public static IHealthChecksBuilder AddRedisCacheHealthCheck(this IHealthChecksBuilder builder) =>
        builder.Add(new HealthCheckRegistration(
            "redis",
            sp => new RedisCacheHealthCheck(sp),
            failureStatus: HealthStatus.Degraded,
            tags: [HealthTags.Ready],
            timeout: TimeSpan.FromSeconds(3)));
}
