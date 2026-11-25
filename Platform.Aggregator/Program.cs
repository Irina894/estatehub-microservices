using System.Net.Security;
using System.Text.Json.Serialization;
using Catalog.Grpc;
using Deals.Grpc;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Grpc;
using Platform.Aggregator.HealthChecks;
using Platform.Aggregator.Resilience;
using Grpc.Health.V1;
using Reviews.Grpc;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<AggregatorOptions>(builder.Configuration.GetSection(AggregatorOptions.SectionName));

// Typed HttpClients: адреса за логічними іменами ресурсів AppHost.
// https+http:// = «HTTPS, якщо доступний, інакше HTTP». CorrelationIdHandler додано в ServiceDefaults.
// Лаб. 7: кожен клієнт має власну політику стійкості (див. таблицю в README):
// Deals і Catalog - критичні (ResilienceProfile.Critical), Reviews - некритичний (NonCritical).
builder.Services.AddHttpClient<DealsClient>(c => c.BaseAddress = new Uri("https+http://deals-api"))
    .AddHttpResilience("deals-api", ResilienceProfile.Critical);
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new Uri("https+http://catalog-api"))
    .AddHttpResilience("catalog-api", ResilienceProfile.Critical);
builder.Services.AddHttpClient<ReviewsClient>(c => c.BaseAddress = new Uri("https+http://reviews-api"))
    .AddHttpResilience("reviews-api", ResilienceProfile.NonCritical);

// ---- gRPC-клієнти (Лаб. 6): згенеровані з .proto; адреса за логічним іменем ресурсу AppHost ----
// gRPC працює поверх HTTP/2, тому звертаємося до HTTPS-endpoint сервісів (HTTP/2 узгоджується через ALPN).
// Налаштування ServiceDefaults (service discovery, resilience, CorrelationIdHandler -> X-Correlation-Id
// у метаданих виклику) діють і тут, бо клієнти створюються тією самою фабрикою HttpClient.
builder.Services.AddTransient<GrpcClientLoggingInterceptor>();
AddGrpcService<DealsService.DealsServiceClient>("deals-api", ResilienceProfile.Critical);
AddGrpcService<CatalogService.CatalogServiceClient>("catalog-api", ResilienceProfile.Critical);
AddGrpcService<ReviewsService.ReviewsServiceClient>("reviews-api", ResilienceProfile.NonCritical);

// ---- Лаб. 7: клієнти gRPC Health Checking Protocol для /health/ready (іменовані, без retry - перевірка має бути швидкою) ----
AddGrpcHealthClient("deals-health", "deals-api");
AddGrpcHealthClient("catalog-health", "catalog-api");
AddGrpcHealthClient("reviews-health", "reviews-api");

builder.Services.AddHealthChecks()
    .AddRedisCacheHealthCheck()                                              // Degraded, якщо Redis недоступний
    .AddGrpcServiceHealthCheck("deals-grpc", "deals-health", critical: true)
    .AddGrpcServiceHealthCheck("catalog-grpc", "catalog-health", critical: true)
    .AddGrpcServiceHealthCheck("reviews-grpc", "reviews-health", critical: false);

// ---- Кеш агрегованих відповідей (Лаб. 6): L1 з обмеженням розміру + Redis через Aspire ----
builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);
// DisableHealthChecks: власну перевірку Redis (Degraded) реєструємо самі, а інтеграційна давала б Unhealthy
builder.AddRedisDistributedCache("redis", s => s.DisableHealthChecks = true);
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<ITwoLevelCache, TwoLevelCache>();

builder.Services.AddScoped<HttpApplicationAggregationService>();   // Лаб. 5, для порівняння (?transport=http)
builder.Services.AddScoped<GrpcApplicationAggregationService>();
builder.Services.AddScoped<CachedApplicationAggregator>();

// camelCase (за замовчуванням) і без null-полів у відповіді
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

ResilienceLog.Initialize(app.Services.GetRequiredService<ILoggerFactory>());   // логер для OnRetry / OnCircuitOpened

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

// Реєстрація gRPC-клієнта з логуванням викликів і політикою стійкості. У Development довіряємо dev-сертифікату
// локальних сервісів, щоб gRPC не залежав від того, чи додано його до довірених у системі (у production цього немає).
void AddGrpcService<TClient>(string service, ResilienceProfile profile) where TClient : class
{
    var clientBuilder = builder.Services
        .AddGrpcClient<TClient>(o => o.Address = new Uri($"https://{service}"))
        .AddInterceptor<GrpcClientLoggingInterceptor>()
        .AddGrpcResilience(service, profile);

    TrustDevCertificate(clientBuilder);
}

void AddGrpcHealthClient(string clientName, string service)
{
    var clientBuilder = builder.Services
        .AddGrpcClient<Health.HealthClient>(clientName, o => o.Address = new Uri($"https://{service}"))
        .RemoveAllResilienceHandlers();   // health-перевірка не повторюється: статус має відображати поточний стан

    TrustDevCertificate(clientBuilder);
}

void TrustDevCertificate(IHttpClientBuilder clientBuilder)
{
    if (builder.Environment.IsDevelopment())
    {
        clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
            SslOptions = new SslClientAuthenticationOptions { RemoteCertificateValidationCallback = (_, _, _, _) => true }
        });
    }
}
