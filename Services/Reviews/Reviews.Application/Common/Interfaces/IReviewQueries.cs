using Reviews.Application.Common.Models;

namespace Reviews.Application.Common.Interfaces;

/// <summary>Читання (Q у CQRS). Реалізація в Infrastructure працює через проєкції/агрегації MongoDB.</summary>
public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct);

    Task<PagedResult<ReviewListItemDto>> GetListAsync(
        long? productId, int? minRating, string? search, int page, int pageSize, CancellationToken ct);
}
