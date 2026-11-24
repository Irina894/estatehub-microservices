using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Platform.ServiceDefaults;

/// <summary>
/// Наскрізний CorrelationId: бере X-Correlation-Id із запиту або генерує новий GUID,
/// кладе його в LogContext (потрапляє в усі логи запиту) і повертає клієнту у відповіді.
/// Відрізняється від TraceId: CorrelationId створює й передає застосунок, TraceId - OpenTelemetry.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var generated = false;
        if (!context.Request.Headers.TryGetValue(HeaderName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            value = Guid.NewGuid().ToString();
            generated = true;
            // запис у Request.Headers: далі його побачать YARP і CorrelationIdHandler
            context.Request.Headers[HeaderName] = value;
        }

        var correlationId = value.ToString();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        var path = context.Request.Path.Value ?? string.Empty;
        // службові запити health checks не засмічують логи
        var isInfrastructure = path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                               || path.StartsWith("/alive", StringComparison.OrdinalIgnoreCase);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            if (isInfrastructure)
            {
                await next(context);
                return;
            }

            if (generated)
                logger.LogInformation("CorrelationId {CorrelationId} generated for {Method} {Path}",
                    correlationId, context.Request.Method, path);

            logger.LogInformation("Request started {Method} {Path} from {ClientIp}",
                context.Request.Method, path, context.Connection.RemoteIpAddress?.ToString());

            await next(context);

            logger.LogInformation("Request finished with {StatusCode}", context.Response.StatusCode);
        }
    }
}

/// <summary>Передає CorrelationId поточного запиту у вихідні HTTP-виклики до інших сервісів.</summary>
public class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var id = accessor.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        if (!string.IsNullOrEmpty(id) && !request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, id);

        return base.SendAsync(request, cancellationToken);
    }
}
