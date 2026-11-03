using AutoMapper;
using MediatR;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Commands.CreateReview;

public sealed class CreateReviewCommandHandler(IReviewRepository repository, IMapper mapper)
    : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken ct)
    {
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
        return mapper.Map<ReviewDto>(review);
    }
}
