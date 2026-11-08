using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Platform.ServiceDefaults;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Спільні налаштування для всіх сервісів платформи (Лаб. 5): Serilog, OpenTelemetry,
/// health checks, service discovery, resilience і наскрізний CorrelationId.
/// Кожен сервіс підключає їх одним викликом builder.AddServiceDefaults().
/// </summary>
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>Назва ActivitySource драйвера MongoDB (пакет MongoDB.Driver.Core.Extensions.DiagnosticSources).</summary>
    private const string MongoActivitySource = "MongoDB.Driver.Core.Extensions.DiagnosticSources";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        // Serilog заміняє стандартне логування; провайдери по черзі очищаємо, щоб Console не дублювався
        builder.Logging.ClearProviders();
        builder.AddSerilogLogging();

        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdHandler>();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();               // retry, timeout, circuit breaker (детально - Лаб. 7)
            http.AddServiceDiscovery();                        // https+http://orders-api -> реальна адреса
            http.AddHttpMessageHandler<CorrelationIdHandler>();// X-Correlation-Id у вихідних запитах
        });

        return builder;
    }

    /// <summary>
    /// Serilog: структуровані записи + збагачення. writeToProviders: true передає записи
    /// далі провайдеру OpenTelemetry, тому вони потрапляють у вкладку Structured logs Dashboard.
    /// </summary>
    public static TBuilder AddSerilogLogging<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var isDevelopment = builder.Environment.IsDevelopment();
        var serviceName = builder.Environment.ApplicationName;

        builder.Services.AddSerilog((services, cfg) => cfg
                .MinimumLevel.Is(isDevelopment ? LogEventLevel.Debug : LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(new CompactJsonFormatter()),
            writeToProviders: true);

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var samplingRatio = builder.Configuration.GetValue("OpenTelemetry:SamplingRatio", 0.1d);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", builder.Environment.EnvironmentName),
                new("service.machine", Environment.MachineName),
            }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing =>
            {
                // 100 % у Development, налаштовуваний відсоток у Production
                if (builder.Environment.IsDevelopment())
                    tracing.SetSampler(new AlwaysOnSampler());
                else
                    tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(samplingRatio)));

                tracing
                    .AddSource(builder.Environment.ApplicationName)   // власні spans сервісу
                    .AddSource("EstateHub.*")                         // ActivitySource бізнес-операцій (EstateHub.Deals тощо)
                    .AddSource(MongoActivitySource)                   // команди MongoDB (Reviews)
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !ctx.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation();                   // і Dapper/ADO.NET (Deals), і EF Core (Catalog)
            });

        // Адресу OTLP (Aspire Dashboard) задає AppHost через OTEL_EXPORTER_OTLP_ENDPOINT
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath);
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
