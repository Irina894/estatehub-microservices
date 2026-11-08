using System.Text.Json.Serialization;
using Platform.Aggregator.Clients;
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

builder.Services.AddScoped<ApplicationAggregationService>();

// camelCase (за замовчуванням) і без null-полів у відповіді
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
