using Deals.Bll.DTOs;
using Deals.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Deals.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RealtorsController(IRealtorService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RealtorDto>>> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RealtorDto>> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<RealtorDto>> Create(CreateRealtorDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RealtorDto>> Update(Guid id, UpdateRealtorDto dto) =>
        Ok(await service.UpdateAsync(id, dto));

    /// <summary>Деактивація (рієлтор пов'язаний з історією заявок, фізично не видаляється).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await service.DeactivateAsync(id);
        return NoContent();
    }
}
