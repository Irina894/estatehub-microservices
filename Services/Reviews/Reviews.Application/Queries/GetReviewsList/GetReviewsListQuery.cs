using FluentValidation;
using MediatR;
using Reviews.Application.Caching;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Messaging;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Queries.GetReviewsList;

public sealed record GetReviewsListQuery(
    long? ProductId, int? MinRating, string? Search, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ReviewListItemDto>>, ICachedQuery
{
    public bool UsesListVersion => true;   // будь-яка зміна відгуків міняє версію й інвалідує всі списки
    public string BuildKey(string listVersion) =>
        ReviewsCacheKeys.List(listVersion, ProductId, MinRating, Search, Page, PageSize);
    public CacheTtl Ttl => new(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));
}

public sealed class GetReviewsListQueryValidator : AbstractValidator<GetReviewsListQuery>
{
    public GetReviewsListQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.MinRating).InclusiveBetween(1, 5).When(x => x.MinRating.HasValue);
        RuleFor(x => x.ProductId).GreaterThan(0).When(x => x.ProductId.HasValue);
    }
}

public sealed class GetReviewsListQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewListItemDto>>
{
    public Task<PagedResult<ReviewListItemDto>> Handle(GetReviewsListQuery r, CancellationToken ct) =>
        queries.GetListAsync(r.ProductId, r.MinRating, r.Search, r.Page, r.PageSize, ct);
}
