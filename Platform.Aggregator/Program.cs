using System.Net.Security;
using System.Text.Json.Serialization;
using Catalog.Grpc;
using Deals.Grpc;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Grpc;
using Reviews.Grpc;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<AggregatorOptions>(builder.Configuration.GetSection(AggregatorOptions.SectionName));

// Typed HttpClients: адреса за логічними іменами ресурсів AppHost.
// https+http:// = «HTTPS, якщо доступний, інакше HTTP». CorrelationIdHandler додано в ServiceDefaults.
builder.Services.AddHttpClient<DealsClient>(c => c.BaseAddress = new Uri("https+http://deals-api"));
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new Uri("https+http://catalog-api"));
builder.Services.AddHttpClient<ReviewsClient>(c => c.BaseAddress = new Uri("https+http://reviews-api"));

// ---- gRPC-клієнти (Лаб. 6): згенеровані з .proto; адреса за логічним іменем ресурсу AppHost ----
// gRPC працює поверх HTTP/2, тому звертаємося до HTTPS-endpoint сервісів (HTTP/2 узгоджується через ALPN).
// Налаштування ServiceDefaults (service discovery, resilience, CorrelationIdHandler -> X-Correlation-Id
// у метаданих виклику) діють і тут, бо клієнти створюються тією самою фабрикою HttpClient.
builder.Services.AddTransient<GrpcClientLoggingInterceptor>();
AddGrpcService<DealsService.DealsServiceClient>("https://deals-api");
AddGrpcService<CatalogService.CatalogServiceClient>("https://catalog-api");
AddGrpcService<ReviewsService.ReviewsServiceClient>("https://reviews-api");

// ---- Кеш агрегованих відповідей (Лаб. 6): L1 з обмеженням розміру + Redis через Aspire ----
builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);
builder.AddRedisDistributedCache("redis");
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<ITwoLevelCache, TwoLevelCache>();

builder.Services.AddScoped<HttpApplicationAggregationService>();   // Лаб. 5, для порівняння (?transport=http)
builder.Services.AddScoped<GrpcApplicationAggregationService>();
builder.Services.AddScoped<CachedApplicationAggregator>();

// camelCase (за замовчуванням) і без null-полів у відповіді
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

// Реєстрація gRPC-клієнта з логуванням викликів. У Development довіряємо dev-сертифікату локальних сервісів,
// щоб gRPC не залежав від того, чи додано його до довірених у системі (у production цього немає).
void AddGrpcService<TClient>(string address) where TClient : class
{
    var clientBuilder = builder.Services
        .AddGrpcClient<TClient>(o => o.Address = new Uri(address))
        .AddInterceptor<GrpcClientLoggingInterceptor>();

    if (builder.Environment.IsDevelopment())
    {
        clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,
            SslOptions = new SslClientAuthenticationOptions { RemoteCertificateValidationCallback = (_, _, _, _) => true }
        });
    }
}
