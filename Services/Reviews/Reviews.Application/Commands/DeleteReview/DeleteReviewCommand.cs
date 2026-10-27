using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common.Messaging;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Commands.DeleteReview;

public sealed record DeleteReviewCommand(string Id) : ICommand;

public sealed class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator() =>
        RuleFor(x => x.Id).Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id має бути коректним ObjectId (24 hex-символи).");
}

public sealed class DeleteReviewCommandHandler(IReviewRepository repository) : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand request, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(ObjectId.Parse(request.Id), ct);
        if (!deleted) throw new NotFoundException(nameof(Review), request.Id);
    }
}
