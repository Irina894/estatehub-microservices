using AutoMapper;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Commands.UpdateReview;

public sealed class UpdateReviewCommandHandler(IReviewRepository repository, IMapper mapper)
    : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(UpdateReviewCommand request, CancellationToken ct)
    {
        var id = ObjectId.Parse(request.Id); // формат уже перевірив валідатор
        var review = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Review), request.Id);

        if (request.ExpectedVersion is { } expected && expected != review.Version)
            throw new ConflictException(
                $"Відгук змінено іншим користувачем: очікувалась версія {expected}, у БД {review.Version}.");

        var criteria = request.Criteria is null
            ? null
            : new Criteria(request.Criteria.Location, request.Criteria.Condition, request.Criteria.Price);

        review.Update(request.Rating, request.Text, criteria);
        await repository.UpdateAsync(review, ct);
        return mapper.Map<ReviewDto>(review);
    }
}
