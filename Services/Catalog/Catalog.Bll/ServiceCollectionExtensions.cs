using Catalog.Bll.Caching;
using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Bll;

public static class ServiceCollectionExtensions
{
    /// <remarks>
    /// IMemoryCache (з SizeLimit) і IDistributedCache (Redis через Aspire) реєструються в Program.cs сервісу;
    /// тут - сам кеш і декоратори над сервісами.
    /// </remarks>
    public static IServiceCollection AddCatalogBll(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(CatalogProfile).Assembly);

        // Валідатори реєструємо вручну-збіркою; викликаємо ЯВНО в контролерах (без AddFluentValidation)
        services.AddValidatorsFromAssemblyContaining<CreatePropertyDtoValidator>();

        // ---- Кеш (Лаб. 6): метрики і дворівневий кеш - singleton, бо кеш спільний для всіх запитів процесу ----
        services.AddSingleton<CacheMetrics>();
        services.AddSingleton<ITwoLevelCache, TwoLevelCache>();

        // ---- Сервіси: справжня реалізація + декоратор з кешем. Споживачі (REST і gRPC) бачать лише інтерфейс ----
        services.AddScoped<PropertyService>();
        services.AddScoped<IPropertyService>(sp =>
            new CachedPropertyService(sp.GetRequiredService<PropertyService>(), sp.GetRequiredService<ITwoLevelCache>()));

        services.AddScoped<CategoryService>();
        services.AddScoped<ICategoryService>(sp =>
            new CachedCategoryService(sp.GetRequiredService<CategoryService>(), sp.GetRequiredService<ITwoLevelCache>()));

        services.AddScoped<IAmenityService, AmenityService>();
        return services;
    }
}
