using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public class AggregatorController(ApplicationAggregationService service) : ControllerBase
{
    /// <summary>Картка заявки: заявка (Deals) + об'єкт (Catalog) + відгуки (Reviews).</summary>
    [HttpGet("applications/{id:guid}/full")]
    [ProducesResponseType(typeof(FullApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FullApplicationDto>> GetFullApplication(Guid id, CancellationToken ct)
    {
        var result = await service.GetFullApplicationAsync(id, ct);
        return result is null ? NotFound(new ProblemDetails { Title = "Application not found", Status = 404 }) : Ok(result);
    }
}
