using System.Diagnostics;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public class AggregatorController(
    CachedApplicationAggregator cached,
    GrpcApplicationAggregationService grpc,
    HttpApplicationAggregationService http) : ControllerBase
{
    /// <summary>
    /// Картка заявки: заявка (Deals) + об'єкт (Catalog) + відгуки (Reviews).
    /// Назовні лишається HTTP, а всередині Aggregator викликає сервіси через gRPC.
    /// </summary>
    /// <param name="transport">grpc (за замовчуванням) або http (Лаб. 5, для порівняння швидкості).</param>
    /// <param name="nocache">true: пропустити кеш агрегата (для чесних замірів і демонстрації trace).</param>
    [HttpGet("applications/{id:guid}/full")]
    [ProducesResponseType(typeof(FullApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<FullApplicationDto>> GetFullApplication(
        Guid id, [FromQuery] string transport = "grpc", [FromQuery] bool nocache = false, CancellationToken ct = default)
    {
        var useHttp = string.Equals(transport, "http", StringComparison.OrdinalIgnoreCase);
        IApplicationAggregator aggregator = useHttp ? http : nocache ? grpc : cached;

        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await aggregator.GetFullApplicationAsync(id, ct);

            // заголовки допомагають порівнювати транспорти: X-Aggregator-Transport та час збирання
            Response.Headers["X-Aggregator-Transport"] = useHttp ? "http" : "grpc";
            Response.Headers["X-Aggregator-Elapsed-Ms"] = Stopwatch.GetElapsedTime(started).TotalMilliseconds.ToString("F1");

            return result is null
                ? NotFound(new ProblemDetails { Title = "Application not found", Status = 404 })
                : Ok(result);
        }
        catch (Exception ex) when (ex is RpcException or HttpRequestException)
        {
            // заявка обов'язкова: якщо Deals недоступний, агрегувати нічого
            return Problem(title: "deals-api is unavailable", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>Видаляє закешовану картку заявки (інвалідація агрегата після зміни вихідних даних).</summary>
    [HttpDelete("applications/{id:guid}/cache")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> InvalidateCache(Guid id, CancellationToken ct)
    {
        await cached.InvalidateAsync(id, ct);
        return NoContent();
    }
}
