using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[Route("api/[controller]")]
public class PropertiesController(
    IPropertyService service,
    IValidator<PropertyQuery> queryValidator,
    IValidator<CreatePropertyDto> createValidator,
    IValidator<UpdatePropertyDto> updateValidator,
    IValidator<ChangePriceDto> priceValidator,
    IValidator<ChangeStatusDto> statusValidator) : ApiControllerBase
{
    /// <summary>Список із фільтрами, сортуванням і пагінацією (усе в SQL).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PropertyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PropertyDto>>> GetAll([FromQuery] PropertyQuery query, CancellationToken ct)
    {
        if (await ValidateAsync(queryValidator, query, ct) is { } bad) return bad;

        var result = await service.GetPagedAsync(query, ct);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result);
    }

    /// <summary>Об'єкт з усіма деталями (Eager Loading).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PropertyDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyDetailsDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await service.GetByIdAsync(id, ct));

    /// <summary>Вільні об'єкти міста від найдешевших (друга специфікація).</summary>
    [HttpGet("featured")]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> GetFeatured([FromQuery] string city, [FromQuery] int limit = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(city))
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["city"] = new[] { "Вкажіть місто." } }));
        return Ok(await service.GetFeaturedAsync(city.Trim(), limit, ct));
    }

    /// <summary>Об'єкти зі зручністю X, додані після дати (LINQ через проміжну сутність M:N).</summary>
    [HttpGet("by-amenity")]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> GetByAmenity([FromQuery] string amenity, [FromQuery] DateTime? since, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(amenity))
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["amenity"] = new[] { "Вкажіть назву зручності." } }));
        return Ok(await service.GetByAmenitySinceAsync(amenity.Trim(), since ?? DateTime.UtcNow.AddYears(-10), ct));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PropertyDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PropertyDetailsDto>> Create([FromBody] CreatePropertyDto dto, CancellationToken ct)
    {
        if (await ValidateAsync(createValidator, dto, ct) is { } bad) return bad;

        var created = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);   // 201 + Location
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyDto dto, CancellationToken ct)
    {
        if (await ValidateAsync(updateValidator, dto, ct) is { } bad) return bad;
        await service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/price")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePrice(Guid id, [FromBody] ChangePriceDto dto, CancellationToken ct)
    {
        if (await ValidateAsync(priceValidator, dto, ct) is { } bad) return bad;
        await service.ChangePriceAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusDto dto, CancellationToken ct)
    {
        if (await ValidateAsync(statusValidator, dto, ct) is { } bad) return bad;
        await service.ChangeStatusAsync(id, dto, ct);
        return NoContent();
    }

    /// <summary>М'яке видалення (IsDeleted = 1).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/amenities/{amenityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddAmenity(Guid id, Guid amenityId, CancellationToken ct)
    {
        await service.AddAmenityAsync(id, amenityId, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/amenities/{amenityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveAmenity(Guid id, Guid amenityId, CancellationToken ct)
    {
        await service.RemoveAmenityAsync(id, amenityId, ct);
        return NoContent();
    }
}
