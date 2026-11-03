using Catalog.Bll.Mapping;
using Catalog.Bll.Services;
using Catalog.Bll.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Bll;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogBll(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(CatalogProfile).Assembly);

        // Валідатори реєструємо вручну-збіркою; викликаємо ЯВНО в контролерах (без AddFluentValidation)
        services.AddValidatorsFromAssemblyContaining<CreatePropertyDtoValidator>();

        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAmenityService, AmenityService>();
        return services;
    }
}
