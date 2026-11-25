using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Platform.Aggregator.Resilience;

/// <summary>
/// Параметри політики стійкості для однієї «категорії» залежності.
/// Критичні сервіси (Deals, Catalog): без їхніх даних відповідь Aggregator не має сенсу, тому
/// терпимо до тимчасових збоїв - більше спроб, довші таймаути, довша вибірка для breaker.
/// Некритичні (Reviews): відповідь можна віддати й без них (partial response + warning), тому
/// швидко здаємося - 1 повтор, короткі таймаути, breaker відкривається легше й надовго.
/// </summary>
public sealed record ResilienceProfile(
    string Name,
    int MaxRetryAttempts,
    TimeSpan RetryDelay,
    TimeSpan AttemptTimeout,
    TimeSpan TotalTimeout,
    TimeSpan SamplingDuration,      // має бути >= 2 × AttemptTimeout (вимога валідації Polly)
    double FailureRatio,
    int MinimumThroughput,
    TimeSpan BreakDuration)
{
    public static readonly ResilienceProfile Critical = new(
        Name: "critical",
        MaxRetryAttempts: 3,
        RetryDelay: TimeSpan.FromMilliseconds(500),
        AttemptTimeout: TimeSpan.FromSeconds(4),
        TotalTimeout: TimeSpan.FromSeconds(15),
        SamplingDuration: TimeSpan.FromSeconds(30),
        FailureRatio: 0.5,
        MinimumThroughput: 10,
        BreakDuration: TimeSpan.FromSeconds(15));

    public static readonly ResilienceProfile NonCritical = new(
        Name: "non-critical",
        MaxRetryAttempts: 1,
        RetryDelay: TimeSpan.FromMilliseconds(200),
        AttemptTimeout: TimeSpan.FromSeconds(2),
        TotalTimeout: TimeSpan.FromSeconds(5),
        SamplingDuration: TimeSpan.FromSeconds(10),
        FailureRatio: 0.3,
        MinimumThroughput: 5,
        BreakDuration: TimeSpan.FromSeconds(30));
}

/// <summary>
/// Статичний логер для колбеків Polly (OnRetry / OnOpened ...): опції Polly створюються до побудови
/// провайдера сервісів, тому логер ініціалізується в Program.cs одразу після builder.Build().
/// </summary>
public static class ResilienceLog
{
    private static ILogger? _logger;

    public static void Initialize(ILoggerFactory factory) => _logger = factory.CreateLogger("Aggregator.Resilience");

    internal static ILogger? Logger => _logger;
}

public static class ResiliencePolicies
{
    // ------------------------------------------------------------------ HTTP

    /// <summary>
    /// HTTP-клієнт зі СВОЄЮ політикою. ConfigureHttpClientDefaults у ServiceDefaults уже додав стандартний
    /// handler, тому спершу видаляємо його (RemoveAllResilienceHandlers) - інакше політики наклалися б
    /// одна на одну (retry × retry, подвійні таймаути).
    /// </summary>
    public static IHttpClientBuilder AddHttpResilience(this IHttpClientBuilder client, string service, ResilienceProfile p)
    {
        client.RemoveAllResilienceHandlers();

        client.AddStandardResilienceHandler(o =>
        {
            // ---- Retry: експоненційна затримка + jitter, лише для безпечних (ідемпотентних) методів ----
            o.Retry.MaxRetryAttempts = p.MaxRetryAttempts;
            o.Retry.Delay = p.RetryDelay;
            o.Retry.BackoffType = DelayBackoffType.Exponential;
            o.Retry.UseJitter = true;                       // розсинхронізує повтори різних запитів
            o.Retry.DisableForUnsafeHttpMethods();          // POST/PUT/PATCH/DELETE не повторюємо
            o.Retry.OnRetry = args =>
            {
                ResilienceLog.Logger?.LogWarning(
                    "[{Profile}] OnRetry {Service}: спроба #{Attempt}, пауза {Delay} мс, причина: {Reason}",
                    p.Name, service, args.AttemptNumber + 1, (int)args.RetryDelay.TotalMilliseconds,
                    args.Outcome.Exception?.GetType().Name ?? args.Outcome.Result?.StatusCode.ToString());
                return default;
            };

            // ---- Timeouts: на одну спробу та на весь запит разом з повторами ----
            o.AttemptTimeout.Timeout = p.AttemptTimeout;
            o.TotalRequestTimeout.Timeout = p.TotalTimeout;

            // ---- Circuit breaker ----
            o.CircuitBreaker.FailureRatio = p.FailureRatio;
            o.CircuitBreaker.MinimumThroughput = p.MinimumThroughput;
            o.CircuitBreaker.SamplingDuration = p.SamplingDuration;
            o.CircuitBreaker.BreakDuration = p.BreakDuration;
            o.CircuitBreaker.OnOpened = args =>
            {
                ResilienceLog.Logger?.LogError(
                    "[{Profile}] OnCircuitOpened {Service}: breaker ВІДКРИТО на {Break} с, причина: {Reason}",
                    p.Name, service, args.BreakDuration.TotalSeconds,
                    args.Outcome.Exception?.GetType().Name ?? args.Outcome.Result?.StatusCode.ToString());
                return default;
            };
            o.CircuitBreaker.OnClosed = _ =>
            {
                ResilienceLog.Logger?.LogInformation("[{Profile}] OnCircuitClosed {Service}: breaker закрито, сервіс відновився", p.Name, service);
                return default;
            };
            o.CircuitBreaker.OnHalfOpened = _ =>
            {
                ResilienceLog.Logger?.LogInformation("[{Profile}] OnCircuitHalfOpened {Service}: пробний запит", p.Name, service);
                return default;
            };
        });

        return client;
    }

    // ------------------------------------------------------------------ gRPC

    /// <summary>
    /// gRPC-клієнт. Повтори - НАТИВНІ gRPC (ServiceConfig/RetryPolicy, за gRPC-статусом), а не Polly:
    /// HTTP-рівень бачить відповідь 200 OK, а помилку gRPC лежить у trailer grpc-status, тому Polly-retry
    /// по HTTP-кодах для gRPC майже не спрацьовує. Тому стандартний handler видаляємо (щоб не було
    /// подвійного retry), а на HTTP-рівні лишаємо лише circuit breaker (для обривів з'єднання).
    /// Таймаут = deadline кожного виклику (див. Deadline() в агрегаторах) - він охоплює і всі повтори.
    /// </summary>
    public static IHttpClientBuilder AddGrpcResilience(this IHttpClientBuilder client, string service, ResilienceProfile p)
    {
        client.RemoveAllResilienceHandlers();

        client.ConfigureChannel(o =>
        {
            o.ServiceConfig = new ServiceConfig
            {
                MethodConfigs =
                {
                    new MethodConfig
                    {
                        Names = { MethodName.Default },     // усі методи сервісу
                        RetryPolicy = new RetryPolicy
                        {
                            MaxAttempts = p.MaxRetryAttempts + 1,               // MaxAttempts рахує й першу спробу
                            InitialBackoff = p.RetryDelay,
                            MaxBackoff = TimeSpan.FromSeconds(4),
                            BackoffMultiplier = 2,                              // експоненційно; gRPC додає jitter сам
                            RetryableStatusCodes = { StatusCode.Unavailable }   // лише «сервіс тимчасово недоступний»
                        }
                    }
                }
            };
        });

        client.AddResilienceHandler($"grpc-breaker-{service}", pipeline =>
        {
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = p.FailureRatio,
                MinimumThroughput = p.MinimumThroughput,
                SamplingDuration = p.SamplingDuration,
                BreakDuration = p.BreakDuration,
                OnOpened = args =>
                {
                    ResilienceLog.Logger?.LogError(
                        "[{Profile}] OnCircuitOpened {Service} (gRPC): breaker ВІДКРИТО на {Break} с, причина: {Reason}",
                        p.Name, service, args.BreakDuration.TotalSeconds,
                        args.Outcome.Exception?.GetType().Name ?? args.Outcome.Result?.StatusCode.ToString());
                    return default;
                },
                OnClosed = _ =>
                {
                    ResilienceLog.Logger?.LogInformation("[{Profile}] OnCircuitClosed {Service} (gRPC)", p.Name, service);
                    return default;
                }
            });
        });

        return client;
    }
}
