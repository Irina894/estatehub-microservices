using FluentValidation;
using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Application.Commands.UpdateReview;

public sealed class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.Id).Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id має бути коректним ObjectId (24 hex-символи).");
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Review.TextMaxLength);
        When(x => x.Criteria is not null, () =>
        {
            RuleFor(x => x.Criteria!.Location).InclusiveBetween(1, 5);
            RuleFor(x => x.Criteria!.Condition).InclusiveBetween(1, 5);
            RuleFor(x => x.Criteria!.Price).InclusiveBetween(1, 5);
        });
    }
}
