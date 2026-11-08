using AutoMapper;
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Commands.CreateReview;

public sealed class CreateReviewCommandHandler(
    IReviewRepository repository, IMapper mapper, ILogger<CreateReviewCommandHandler> logger)
    : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    private static readonly ActivitySource Source = new("EstateHub.Reviews");

    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        using var activity = Source.StartActivity("CreateReview");
        activity?.SetTag("review.productId", request.ProductId);

        var criteria = request.Criteria is null
            ? null
            : new Criteria(request.Criteria.Location, request.Criteria.Condition, request.Criteria.Price);

        var review = new Review(
            request.ProductId,
            request.ProductName,
            new Author(request.AuthorUserId, request.AuthorDisplayName),
            request.Rating,
            request.Text,
            criteria);

        await repository.AddAsync(review, ct);
        logger.LogInformation("Review {ReviewId} created for product {ProductId}, rating {Rating}",
            review.Id.ToString(), request.ProductId, request.Rating);
        return mapper.Map<ReviewDto>(review);
    }
}
