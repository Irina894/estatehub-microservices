using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Behaviors;

/// <summary>Зовнішній behavior: логує початок і кінець обробки кожного запиту.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("[START] {Request}", name);
        try
        {
            var response = await next();
            logger.LogInformation("[END] {Request}", name);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogWarning("[FAIL] {Request}: {Error}", name, ex.GetType().Name);
            throw;
        }
    }
}
