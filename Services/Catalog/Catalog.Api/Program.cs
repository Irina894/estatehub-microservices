using Catalog.Api.ExceptionHandling;
using Catalog.Api.Middleware;
using Catalog.Bll;
using Catalog.Dal;
using Catalog.Dal.Repositories;
using Catalog.Dal.Seeding;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog: налаштування в appsettings.json (Console + File, SQL-команди EF) ----
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// ---- DAL ----
var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Не задано ConnectionStrings:CatalogDb");

builder.Services.AddDbContext<CatalogDbContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ---- BLL ----
builder.Services.AddCatalogBll();

// ---- API ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ProblemDetails + глобальний обробник помилок
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// ---- Pipeline (порядок важливий) ----
app.UseMiddleware<CorrelationIdMiddleware>();   // 1) щоб CorrelationId був у всіх логах
app.UseSerilogRequestLogging();                  // 2) один структурований лог на запит
app.UseExceptionHandler();                       // 3) ловить винятки з контролерів -> ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// ---- Міграції та сідінг (вимкнено в Production) ----
if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

    if (app.Configuration.GetValue("Database:ApplyMigrations", true))
        await db.Database.MigrateAsync();

    if (app.Configuration.GetValue("Database:Seed", true))
        await DbInitializer.SeedAsync(db);
}

app.Run();

public partial class Program;
