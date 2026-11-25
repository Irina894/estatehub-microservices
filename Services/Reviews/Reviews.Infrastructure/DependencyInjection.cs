using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Common.Interfaces;
using Reviews.Domain.Interfaces;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Seeding;

namespace Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        MongoMappings.Register();

        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));

        // Aspire передає рядок підключення як ConnectionStrings:ReviewsDb і має пріоритет;
        // без Aspire лишається MongoDb:ConnectionString з appsettings.json.
        services.PostConfigure<MongoDbOptions>(o =>
        {
            var fromAspire = configuration.GetConnectionString("ReviewsDb");
            if (!string.IsNullOrWhiteSpace(fromAspire)) o.ConnectionString = fromAspire;
        });
        services.AddSingleton<MongoDbContext>();

        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();

        services.AddScoped<IDataSeeder, ReviewSeeder>();
        services.AddHostedService<IndexCreationService>();
        return services;
    }
}
