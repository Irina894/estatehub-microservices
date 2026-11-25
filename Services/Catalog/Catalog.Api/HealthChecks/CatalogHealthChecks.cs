using Catalog.Dal;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.ServiceDefaults;

namespace Catalog.Api.HealthChecks;

/// <summary>
/// Лаб. 7: readiness-перевірка SQL Server для Catalog (EF Core: Database.CanConnectAsync).
/// CatalogDbContext - Scoped, тому перевірка (Transient) отримує його через DI на кожен виклик.
/// </summary>
public sealed class CatalogSqlHealthCheck(CatalogDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy("SQL Server (CatalogDb) доступний")
                : HealthCheckResult.Unhealthy("SQL Server (CatalogDb) недоступний");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("SQL Server (CatalogDb) недоступний", ex);
        }
    }
}

public static class CatalogHealthCheckExtensions
{
    public static IHealthChecksBuilder AddCatalogHealthChecks(this IHealthChecksBuilder builder) =>
        builder
            .AddCheck<CatalogSqlHealthCheck>("sqlserver", HealthStatus.Unhealthy, [HealthTags.Ready], TimeSpan.FromSeconds(5))
            .AddRedisCacheHealthCheck();
}
