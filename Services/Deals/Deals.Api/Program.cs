using Deals.Api.Grpc;
using Deals.Api.Middleware;
using Deals.Bll.Caching;
using Deals.Bll.Mapping;
using Deals.Bll.Services;
using Deals.Dal;
using Deals.Domain.Interfaces;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();   // Serilog, OpenTelemetry, health checks, service discovery (Лаб. 5)

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ProblemDetails + централізований обробник винятків
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Рядок підключення: "DealsDb" (Aspire передає його як ConnectionStrings:DealsDb)
var connectionString = builder.Configuration.GetConnectionString("DealsDb")
    ?? throw new InvalidOperationException("Connection string 'DealsDb' is not configured.");

// Unit of Work: одне з'єднання і одна транзакція на запит; звільняється разом зі scope
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));

// ---- Кеш (Лаб. 6) ----
// L1: кеш у пам'яті з обмеженням розміру (кожен запис має Size = 1, тож максимум 10 000 записів)
builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);
// L2: Redis через Aspire (ім'я "redis" = ім'я ресурсу в AppHost)
builder.AddRedisDistributedCache("redis");
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<ITwoLevelCache, TwoLevelCache>();

// ---- BLL: справжні сервіси + декоратори з кешем ----
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<IApplicationService>(sp =>
    new CachedApplicationService(sp.GetRequiredService<ApplicationService>(), sp.GetRequiredService<ITwoLevelCache>()));
builder.Services.AddScoped<RealtorService>();
builder.Services.AddScoped<IRealtorService>(sp =>
    new CachedRealtorService(sp.GetRequiredService<RealtorService>(), sp.GetRequiredService<ITwoLevelCache>()));

// ---- gRPC (контракт Protos/deals.proto) + interceptor логування з ServiceDefaults ----
builder.Services.AddServiceGrpc();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();   // CorrelationId у всіх логах запиту

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGrpcService<DealsGrpcService>();
app.MapDefaultEndpoints();                       // /health і /alive

app.Run();
