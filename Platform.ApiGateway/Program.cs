using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Platform.ServiceDefaults;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// YARP: маршрути й кластери з appsettings.json; адреси http://deals-api тощо
// перетворює на реальні service discovery Aspire
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

// 1) CorrelationId: ДО MapReverseProxy, щоб ідентифікатор потрапив у проксований запит
app.UseMiddleware<CorrelationIdMiddleware>();

// 2) Логування рішень маршрутизації: метод, шлях, маршрут, статус, тривалість
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    try
    {
        await next();
    }
    finally
    {
        sw.Stop();
        var routeId = context.Features.Get<IReverseProxyFeature>()?.Route.Config.RouteId ?? "none";
        var level = context.Response.StatusCode >= 500 ? LogLevel.Error : LogLevel.Information;
        app.Logger.Log(level,
            "{Method} {Path} -> route {Route}: {StatusCode} in {Elapsed} ms",
            context.Request.Method, context.Request.Path.Value, routeId,
            context.Response.StatusCode, sw.ElapsedMilliseconds);
    }
});

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
