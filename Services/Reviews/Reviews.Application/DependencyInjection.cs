using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Behaviors;
using Reviews.Application.Caching;

namespace Reviews.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Порядок реєстрації = порядок виконання: Logging -> Validation -> Caching -> Invalidation -> Performance -> handler
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            // Кеш (Лаб. 6): після валідації, щоб некоректні запити не потрапляли в кеш; DI створює ці behaviors лише для
            // запитів з ICachedQuery / команд з ICacheInvalidating (інші не задовольняють обмеження типу).
            cfg.AddOpenBehavior(typeof(QueryCachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        // IMemoryCache (з SizeLimit) і IDistributedCache (Redis) реєструє Program.cs веб-API
        services.AddSingleton<CacheMetrics>();
        services.AddSingleton<ITwoLevelCache, TwoLevelCache>();

        services.AddValidatorsFromAssembly(assembly);
        services.AddAutoMapper(assembly);
        return services;
    }
}
