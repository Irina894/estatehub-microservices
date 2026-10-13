using Deals.Api.Middleware;
using Deals.Bll.Caching;
using Deals.Bll.Mapping;
using Deals.Bll.Services;
using Deals.Dal;
using Deals.Domain.Interfaces;

var builder = WebApplication.CreateBuilder(args);

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

// BLL
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<RealtorService>();
builder.Services.AddScoped<IRealtorService>(sp =>
    new CachedRealtorService(sp.GetRequiredService<RealtorService>(), sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
