using FluentValidation;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Commands.CreateReview;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator(IReviewRepository repository)
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AuthorUserId).GreaterThan(0);
        RuleFor(x => x.AuthorDisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Review.TextMaxLength);

        When(x => x.Criteria is not null, () =>
        {
            RuleFor(x => x.Criteria!.Location).InclusiveBetween(1, 5);
            RuleFor(x => x.Criteria!.Condition).InclusiveBetween(1, 5);
            RuleFor(x => x.Criteria!.Price).InclusiveBetween(1, 5);
        });

        // асинхронне правило: один користувач — один відгук на об'єкт
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) => !await repository.ExistsAsync(cmd.ProductId, cmd.AuthorUserId, ct))
            .OverridePropertyName("Review")
            .WithMessage("Цей користувач уже залишив відгук про цей об'єкт.");
    }
}
