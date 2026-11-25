using Catalog.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.ExceptionHandling;

/// <summary>
/// Єдине місце мапінгу винятків у HTTP-відповіді формату ProblemDetails (RFC 7807).
/// EntityNotFound -> 404, Validation -> 400, Conflict/Domain/Concurrency -> 409, інше -> 500.
/// Помилка логується один раз (тут), а не в кожному шарі.
/// </summary>
public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetails,
    IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case EntityNotFoundException:
                problem = Build(StatusCodes.Status404NotFound, "Не знайдено", exception.Message);
                logger.LogWarning(exception, "Not found: {Message}", exception.Message);
                break;

            case ValidationException ve:
                var errors = ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                problem = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Помилка валідації"
                };
                logger.LogWarning("Validation failed: {@Errors}", errors);
                break;

            case BadHttpRequestException:
                problem = Build(StatusCodes.Status400BadRequest, "Некоректний запит", exception.Message);
                logger.LogWarning(exception, "Bad request: {Message}", exception.Message);
                break;

            case ConflictException or DomainException:
                problem = Build(StatusCodes.Status409Conflict, "Конфлікт", exception.Message);
                logger.LogWarning(exception, "Conflict: {Message}", exception.Message);
                break;

            case DbUpdateConcurrencyException:
                problem = Build(StatusCodes.Status409Conflict, "Конфлікт паралельного редагування",
                    "Запис змінено іншим користувачем. Завантажте дані ще раз і повторіть.");
                logger.LogWarning(exception, "Concurrency conflict");
                break;

            default:
                problem = Build(StatusCodes.Status500InternalServerError, "Внутрішня помилка сервера",
                    env.IsDevelopment() ? exception.ToString() : "Сталася неочікувана помилка.");
                logger.LogError(exception, "Unhandled exception");
                break;
        }

        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Build(int status, string title, string detail)
        => new() { Status = status, Title = title, Detail = detail };
}
