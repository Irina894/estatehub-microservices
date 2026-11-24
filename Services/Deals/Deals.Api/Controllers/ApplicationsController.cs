using Deals.Bll.DTOs;
using Deals.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Deals.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController(IApplicationService service) : ControllerBase
{
    /// <summary>Список заявок із пагінацією та фільтром за статусом.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ApplicationDto>>> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null) =>
        Ok(await service.GetPagedAsync(page, pageSize, status));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationDto>> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<ApplicationDto>> Create(CreateApplicationDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Відхилення заявки (перевіряється допустимість переходу та RowVersion).</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApplicationDto>> ChangeStatus(Guid id, ChangeApplicationStatusDto dto) =>
        Ok(await service.ChangeStatusAsync(id, dto));

    /// <summary>М'яке видалення заявки.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Транзакція: затвердження заявки + створення огляду.</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ViewingDto>> Approve(Guid id, ApproveApplicationDto dto) =>
        Ok(await service.ApproveAsync(id, dto));

    /// <summary>Транзакція: підписання договору + завершення заявки.</summary>
    [HttpPost("{id:guid}/contract")]
    public async Task<ActionResult<ContractDto>> SignContract(Guid id, SignContractDto dto) =>
        Ok(await service.SignContractAsync(id, dto));

    [HttpGet("{id:guid}/contract")]
    public async Task<ActionResult<ContractDto>> GetContract(Guid id) => Ok(await service.GetContractAsync(id));

    [HttpGet("{id:guid}/viewings")]
    public async Task<ActionResult<IReadOnlyList<ViewingDto>>> GetViewings(Guid id) =>
        Ok(await service.GetViewingsAsync(id));

    /// <summary>M:N: призначення рієлтора на заявку (listing_agent / buyer_agent).</summary>
    [HttpPost("{id:guid}/realtors")]
    public async Task<ActionResult<ApplicationRealtorDto>> AssignRealtor(Guid id, AssignRealtorDto dto) =>
        Ok(await service.AssignRealtorAsync(id, dto));

    [HttpGet("{id:guid}/realtors")]
    public async Task<ActionResult<IReadOnlyList<ApplicationRealtorDto>>> GetRealtors(Guid id) =>
        Ok(await service.GetRealtorsAsync(id));
}
