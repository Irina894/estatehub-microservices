using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.ServiceDefaults;

namespace Deals.Api.HealthChecks;

/// <summary>
/// Лаб. 7: readiness-перевірка SQL Server (БД Deals). Виконує «SELECT 1» з таймаутом 3 с.
/// SQL - обов'язкова залежність, тому збій = Unhealthy (сервіс не готовий приймати трафік, /health/ready -> 503).
/// </summary>
public sealed class SqlServerHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var connectionString = configuration.GetConnectionString("DealsDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            return HealthCheckResult.Unhealthy("ConnectionStrings:DealsDb не налаштовано");

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 3;
            await command.ExecuteScalarAsync(ct);

            return HealthCheckResult.Healthy("SQL Server (DealsDb) доступний");
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("SQL Server (DealsDb) недоступний", ex);
        }
    }
}

public static class DealsHealthCheckExtensions
{
    /// <summary>SQL Server (Unhealthy при збої) + Redis (Degraded при збої); обидві з тегом ready.</summary>
    public static IHealthChecksBuilder AddDealsHealthChecks(this IHealthChecksBuilder builder) =>
        builder
            .AddCheck<SqlServerHealthCheck>("sqlserver", HealthStatus.Unhealthy, [HealthTags.Ready], TimeSpan.FromSeconds(5))
            .AddRedisCacheHealthCheck();
}
