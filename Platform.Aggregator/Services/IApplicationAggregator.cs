using Platform.Aggregator.Models;

namespace Platform.Aggregator.Services;

/// <summary>Збирає «картку заявки». Є дві реалізації транспорту (HTTP з Лаб. 5 і gRPC з Лаб. 6) та кеш-декоратор.</summary>
public interface IApplicationAggregator
{
    /// <returns>null, якщо заявки не існує.</returns>
    Task<FullApplicationDto?> GetFullApplicationAsync(Guid applicationId, CancellationToken ct);
}
