using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.ServiceDefaults;

namespace Platform.Aggregator.HealthChecks;

/// <summary>
/// Лаб. 7: Aggregator перевіряє залежні gRPC-сервіси стандартним Health Checking Protocol
/// (Health/Check з дедлайном 2 с, щоб /health/ready не «висів»).
/// Критичний сервіс (Deals, Catalog) недоступний -> Unhealthy (агрегатор не може працювати);
/// некритичний (Reviews) -> Degraded (відповідь можлива, але з попередженням).
/// </summary>
public sealed class GrpcServiceHealthCheck(GrpcClientFactory factory, string clientName, bool critical) : IHealthCheck
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(2);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var failure = critical ? HealthStatus.Unhealthy : HealthStatus.Degraded;
        try
        {
            var client = factory.CreateClient<Health.HealthClient>(clientName);
            var reply = await client.CheckAsync(
                new HealthCheckRequest { Service = string.Empty },    // "" = загальний стан сервера
                deadline: DateTime.UtcNow.Add(Deadline),
                cancellationToken: ct);

            return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy($"{clientName}: SERVING")
                : new HealthCheckResult(failure, $"{clientName}: {reply.Status}");
        }
        catch (RpcException ex)
        {
            return new HealthCheckResult(failure, $"{clientName}: gRPC {ex.StatusCode}", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new HealthCheckResult(failure, $"{clientName}: недоступний", ex);
        }
    }
}

public static class GrpcHealthRegistration
{
    public static IHealthChecksBuilder AddGrpcServiceHealthCheck(
        this IHealthChecksBuilder builder, string name, string clientName, bool critical) =>
        builder.Add(new HealthCheckRegistration(
            name,
            sp => new GrpcServiceHealthCheck(sp.GetRequiredService<GrpcClientFactory>(), clientName, critical),
            failureStatus: null,
            tags: [HealthTags.Ready],
            timeout: TimeSpan.FromSeconds(4)));
}
