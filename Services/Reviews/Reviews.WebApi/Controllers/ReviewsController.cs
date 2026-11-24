using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Commands.AddComment;
using Reviews.Application.Commands.CreateReview;
using Reviews.Application.Commands.DeleteReview;
using Reviews.Application.Commands.UpdateReview;
using Reviews.Application.Common.Models;
using Reviews.Application.Queries.GetReviewById;
using Reviews.Application.Queries.GetReviewsList;
using Reviews.WebApi.Contracts;

namespace Reviews.WebApi.Controllers;

/// <summary>Тонкий контролер: лише перетворює HTTP у команди/запити MediatR і віддає результат.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ReviewsController(ISender sender) : ControllerBase
{
    /// <summary>Список відгуків (фільтри, пошук, пагінація).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReviewListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReviewListItemDto>>> GetList(
        [FromQuery] long? productId, [FromQuery] int? minRating, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await sender.Send(new GetReviewsListQuery(productId, minRating, search, page, pageSize), ct));

    /// <summary>Відгук за ідентифікатором (ObjectId).</summary>
    [HttpGet("{id}", Name = "GetReviewById")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> GetById(string id, CancellationToken ct)
    {
        var review = await sender.Send(new GetReviewByIdQuery(id), ct);
        Response.Headers.ETag = $"\"{review.Version}\"";
        return Ok(review);
    }

    /// <summary>Створити відгук.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Create([FromBody] CreateReviewRequest body, CancellationToken ct)
    {
        var created = await sender.Send(new CreateReviewCommand(
            body.ProductId, body.ProductName, body.AuthorUserId, body.AuthorDisplayName,
            body.Rating, body.Text, body.Criteria), ct);
        return CreatedAtRoute("GetReviewById", new { id = created.Id }, created);
    }

    /// <summary>Оновити відгук. Якщо передано version і вона застаріла, повертається 409.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Update(string id, [FromBody] UpdateReviewRequest body, CancellationToken ct) =>
        Ok(await sender.Send(new UpdateReviewCommand(id, body.Rating, body.Text, body.Criteria, body.Version), ct));

    /// <summary>Додати коментар до відгуку.</summary>
    [HttpPost("{id}/comments")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> AddComment(string id, [FromBody] AddCommentRequest body, CancellationToken ct) =>
        Ok(await sender.Send(new AddCommentCommand(id, body.AuthorId, body.Text), ct));

    /// <summary>Видалити відгук.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await sender.Send(new DeleteReviewCommand(id), ct);
        return NoContent();
    }
}
