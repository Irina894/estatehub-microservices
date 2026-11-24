using Reviews.Application;
using Reviews.WebApi.Grpc;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Seeding;
using Platform.ServiceDefaults;
using Reviews.WebApi.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();   // Serilog, OpenTelemetry, health checks, service discovery (Лаб. 5)

// ---- Кеш (Лаб. 6) ----
// L1: кеш у пам'яті з обмеженням розміру (кожен запис має Size = 1, тож максимум 10 000 записів)
builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);
// L2: Redis через Aspire (ім'я "redis" = ім'я ресурсу в AppHost)
builder.AddRedisDistributedCache("redis");

builder.Services.AddApplication();   // MediatR + behaviors кешу
builder.Services.AddInfrastructure(builder.Configuration);

// ---- gRPC (контракт Protos/reviews.proto) + interceptor логування з ServiceDefaults ----
builder.Services.AddServiceGrpc();

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGrpcService<ReviewsGrpcService>();
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
