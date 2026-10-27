using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[Route("api/[controller]")]
public class AmenitiesController(IAmenityService service, IValidator<CreateAmenityDto> validator) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AmenityDto>>> GetAll(CancellationToken ct)
        => Ok(await service.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AmenityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AmenityDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(typeof(AmenityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AmenityDto>> Create([FromBody] CreateAmenityDto dto, CancellationToken ct)
    {
        if (await ValidateAsync(validator, dto, ct) is { } bad) return bad;

        var created = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
