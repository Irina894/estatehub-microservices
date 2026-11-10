using FluentValidation;
using MediatR;
using MongoDB.Bson;
using Microsoft.Extensions.Caching.Memory;
using Reviews.Application.Caching;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Messaging;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;

namespace Reviews.Application.Queries.GetReviewById;

public sealed record GetReviewByIdQuery(string Id) : IQuery<ReviewDto>, ICachedQuery
{
    public bool UsesListVersion => false;
    public string BuildKey(string listVersion) => ReviewsCacheKeys.Review(Id);
    public CacheTtl Ttl => new(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
}

public sealed class GetReviewByIdQueryValidator : AbstractValidator<GetReviewByIdQuery>
{
    public GetReviewByIdQueryValidator() =>
        RuleFor(x => x.Id).Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id має бути коректним ObjectId (24 hex-символи).");
}

public sealed class GetReviewByIdQueryHandler(IReviewQueries queries) : IRequestHandler<GetReviewByIdQuery, ReviewDto>
{
    public async Task<ReviewDto> Handle(GetReviewByIdQuery request, CancellationToken ct) =>
        await queries.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException(nameof(Review), request.Id);
}
