using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Behaviors;

/// <summary>Внутрішній behavior: міряє час handler-а і попереджає, якщо він довший за 500 мс.</summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private const int ThresholdMs = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > ThresholdMs)
            logger.LogWarning("Повільний запит {Request}: {Elapsed} мс (поріг {Threshold} мс)",
                typeof(TRequest).Name, sw.ElapsedMilliseconds, ThresholdMs);

        return response;
    }
}
