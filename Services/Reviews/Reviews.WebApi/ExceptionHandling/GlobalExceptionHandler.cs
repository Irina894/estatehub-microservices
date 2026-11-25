using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Domain.Exceptions;

namespace Reviews.WebApi.ExceptionHandling;

/// <summary>Перетворює винятки на RFC 7807 ProblemDetails з правильним HTTP-статусом.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var problem = new ProblemDetails { Instance = ctx.Request.Path };

        switch (ex)
        {
            case ValidationException ve:
                problem.Status = StatusCodes.Status400BadRequest;
                problem.Title = "Помилка валідації";
                problem.Extensions["errors"] = ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                break;
            case DomainException de:
                problem.Status = StatusCodes.Status400BadRequest;
                problem.Title = "Порушення бізнес-правила";
                problem.Detail = de.Message;
                break;
            case NotFoundException nf:
                problem.Status = StatusCodes.Status404NotFound;
                problem.Title = "Ресурс не знайдено";
                problem.Detail = nf.Message;
                break;
            case ConflictException ce:
                problem.Status = StatusCodes.Status409Conflict;
                problem.Title = "Конфлікт";
                problem.Detail = ce.Message;
                break;
            case MongoWriteException mwe when mwe.WriteError.Category == ServerErrorCategory.DuplicateKey:
                problem.Status = StatusCodes.Status409Conflict;
                problem.Title = "Дублікат";
                problem.Detail = "Документ з такими унікальними значеннями вже існує (порушено унікальний індекс).";
                break;
            case MongoWriteException:
                problem.Status = StatusCodes.Status400BadRequest;
                problem.Title = "Помилка запису в MongoDB";
                problem.Detail = "Документ не пройшов перевірку бази даних.";
                break;
            case MongoConnectionException or TimeoutException or MongoWaitQueueFullException:
                problem.Status = StatusCodes.Status503ServiceUnavailable;
                problem.Title = "База даних тимчасово недоступна";
                problem.Detail = "Спробуйте повторити запит пізніше.";
                break;
            default:
                problem.Status = StatusCodes.Status500InternalServerError;
                problem.Title = "Внутрішня помилка сервера";
                break;
        }

        if (problem.Status >= 500) logger.LogError(ex, "Необроблений виняток");
        else logger.LogWarning("{Type}: {Message}", ex.GetType().Name, ex.Message);

        ctx.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            ProblemDetails = problem,
            Exception = ex
        });
    }
}
