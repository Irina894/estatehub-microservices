using AutoMapper;
using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Messaging;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Commands.AddComment;

public sealed record AddCommentCommand(string ReviewId, long AuthorId, string Text) : ICommand<ReviewDto>;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.ReviewId).Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id має бути коректним ObjectId.");
        RuleFor(x => x.AuthorId).GreaterThan(0);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Comment.TextMaxLength);
    }
}

public sealed class AddCommentCommandHandler(IReviewRepository repository, IMapper mapper)
    : IRequestHandler<AddCommentCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(AddCommentCommand request, CancellationToken ct)
    {
        var review = await repository.GetByIdAsync(ObjectId.Parse(request.ReviewId), ct)
                     ?? throw new NotFoundException(nameof(Review), request.ReviewId);
        review.AddComment(request.AuthorId, request.Text);
        await repository.UpdateAsync(review, ct);
        return mapper.Map<ReviewDto>(review);
    }
}
