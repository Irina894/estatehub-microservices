using Deals.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Deals.Api.Middleware;

/// <summary>Централізована обробка помилок: усі винятки перетворюються на ProblemDetails (RFC 7807).</summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title, detail) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", ex.Message),
            BusinessConflictException => (StatusCodes.Status409Conflict, "Business Conflict", ex.Message),
            DomainValidationException => (StatusCodes.Status400BadRequest, "Validation Error", ex.Message),
            // порушення UNIQUE (2627, 2601) та FOREIGN KEY / CHECK (547) на рівні БД
            SqlException { Number: 2627 or 2601 } => (StatusCodes.Status409Conflict, "Duplicate Value",
                "A record with the same unique value already exists."),
            SqlException { Number: 547 } => (StatusCodes.Status409Conflict, "Constraint Violation",
                "The operation violates a database constraint."),
            _ => (StatusCodes.Status500InternalServerError, "Server Error", "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Unhandled exception");

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = ctx.Request.Path
        }, ct);
        return true;
    }
}
