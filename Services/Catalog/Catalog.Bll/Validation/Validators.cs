using Catalog.Bll.Dtos;
using Catalog.Domain.Enums;
using FluentValidation;

namespace Catalog.Bll.Validation;

public class CreatePropertyDtoValidator : AbstractValidator<CreatePropertyDto>
{
    public CreatePropertyDtoValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.City).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0).PrecisionScale(18, 2, false)
            .WithMessage("Ціна має бути більшою за 0 і мати не більше 2 знаків після коми.");

        RuleForEach(x => x.ImageUrls)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Кожне посилання на зображення має бути коректним http(s) URL.");

        When(x => x.Location is not null, () =>
        {
            RuleFor(x => x.Location!.FullAddress).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Location!.Latitude).InclusiveBetween(-90, 90).When(x => x.Location!.Latitude.HasValue);
            RuleFor(x => x.Location!.Longitude).InclusiveBetween(-180, 180).When(x => x.Location!.Longitude.HasValue);
            RuleFor(x => x.Location!)
                .Must(l => l.Latitude.HasValue == l.Longitude.HasValue)
                .WithMessage("Широту і довготу потрібно вказувати разом.");
        });
    }
}

public class UpdatePropertyDtoValidator : AbstractValidator<UpdatePropertyDto>
{
    public UpdatePropertyDtoValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.City).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}

public class ChangePriceDtoValidator : AbstractValidator<ChangePriceDto>
{
    public ChangePriceDtoValidator()
    {
        RuleFor(x => x.Price).GreaterThan(0).PrecisionScale(18, 2, false)
            .WithMessage("Ціна має бути більшою за 0 і мати не більше 2 знаків після коми.");
    }
}

public class ChangeStatusDtoValidator : AbstractValidator<ChangeStatusDto>
{
    public ChangeStatusDtoValidator()
    {
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => Enum.TryParse<PropertyStatus>(s, true, out _))
            .WithMessage("Допустимі статуси: available, reserved, sold, rented.");
    }
}

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator() => RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
}

public class CreateAmenityDtoValidator : AbstractValidator<CreateAmenityDto>
{
    public CreateAmenityDtoValidator() => RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
}

public class PropertyQueryValidator : AbstractValidator<PropertyQuery>
{
    private static readonly string[] SortFields = { "title", "price", "city", "createdat" };

    public PropertyQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PropertyQuery.MaxPageSize);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x).Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithName("MinPrice").WithMessage("MinPrice не може бути більшим за MaxPrice.");
        RuleFor(x => x.SortBy).Must(s => SortFields.Contains(s.ToLowerInvariant()))
            .WithMessage("SortBy: title, price, city або createdAt.");
        RuleFor(x => x.SortDir).Must(s => s.Equals("asc", StringComparison.OrdinalIgnoreCase) || s.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDir: asc або desc.");
        RuleFor(x => x.Status).Must(s => Enum.TryParse<PropertyStatus>(s, true, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Допустимі статуси: available, reserved, sold, rented.");
    }
}
