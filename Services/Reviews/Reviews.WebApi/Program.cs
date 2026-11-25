using Reviews.Application;
using Reviews.WebApi.Grpc;
using Reviews.WebApi.HealthChecks;
using Reviews.WebApi.Metrics;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Seeding;
using Platform.ServiceDefaults;
using Reviews.WebApi.ExceptionHandling;
using Reviews.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();   // Serilog, OpenTelemetry, health checks, service discovery (Лаб. 5)

// ---- Кеш (Лаб. 6) ----
// L1: кеш у пам'яті з обмеженням розміру (кожен запис має Size = 1, тож максимум 10 000 записів)
builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);
// L2: Redis через Aspire (ім'я "redis" = ім'я ресурсу в AppHost)
// DisableHealthChecks: стандартна перевірка Aspire дала б Unhealthy; ми реєструємо власну (Degraded) нижче
builder.AddRedisDistributedCache("redis", s => s.DisableHealthChecks = true);

builder.Services.AddApplication();   // MediatR + behaviors кешу
builder.Services.AddInfrastructure(builder.Configuration);

// ---- gRPC (контракт Protos/reviews.proto) + interceptor логування з ServiceDefaults ----
builder.Services.AddServiceGrpc();

// ---- Метрики та health checks (Лаб. 7) ----
builder.Services.AddSingleton<ReviewsMetrics>();
builder.Services.AddHealthChecks().AddReviewsHealthChecks();
builder.Services.AddGrpcHealthChecks();                                  // стандартний gRPC Health Checking Protocol

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

// Лаб. 7: імітація збоїв для демонстрації retry / circuit breaker (лише Development, за замовчуванням нічого не робить)
if (app.Environment.IsDevelopment())
    app.UseMiddleware<FaultInjectionMiddleware>();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGrpcService<ReviewsGrpcService>();
app.MapGrpcHealthChecksService();                // grpc.health.v1.Health/Check
app.MapDefaultEndpoints();

// Початкові дані (ідемпотентно). Якщо MongoDB не запущена, додаток все одно стартує.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await scope.ServiceProvider.GetRequiredService<IDataSeeder>().SeedAsync(CancellationToken.None);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Seed не виконано (MongoDB недоступна?).");
    }
}

app.Run();

public partial class Program { }
