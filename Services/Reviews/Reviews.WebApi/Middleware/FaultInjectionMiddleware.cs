namespace Reviews.WebApi.Middleware;

/// <summary>
/// ЛИШЕ ДЛЯ ДЕМОНСТРАЦІЇ Лаб. 7 (підключається тільки в Development): імітує збої Reviews, щоб у логах
/// Aggregator з'явилися OnRetry / OnCircuitOpened, а відповідь стала partial (warning).
/// Керується конфігурацією (за замовчуванням нічого не ламає):
///   FaultInjection:FailureRate  0..1  - частка запитів, що отримають 503 (напр. 0.7);
///   FaultInjection:LatencyMs    мс    - штучна затримка кожного запиту (перевірка таймаутів).
/// Health-ендпоінти не чіпаємо, щоб сервіс лишався «живим».
/// </summary>
public sealed class FaultInjectionMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<FaultInjectionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/alive") || path.StartsWithSegments("/grpc.health.v1.Health"))
        {
            await next(context);
            return;
        }

        var latency = configuration.GetValue("FaultInjection:LatencyMs", 0);
        if (latency > 0)
            await Task.Delay(latency, context.RequestAborted);

        var failureRate = configuration.GetValue("FaultInjection:FailureRate", 0d);
        if (failureRate > 0 && Random.Shared.NextDouble() < failureRate)
        {
            logger.LogWarning("FaultInjection: відповідаю 503 на {Path} (FailureRate={Rate})", path, failureRate);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        await next(context);
    }
}
