using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Явний виклик FluentValidation (без AddFluentValidation).
    /// Якщо помилки є — повертає 400 із ValidationProblemDetails, інакше null.
    /// </summary>
    protected async Task<ActionResult?> ValidateAsync<T>(IValidator<T> validator, T dto, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(dto, ct);
        if (result.IsValid) return null;

        return ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
    }
}
