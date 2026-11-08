using Reviews.Application.Common.Models;

namespace Reviews.WebApi.Contracts;

public sealed record CreateReviewRequest(
    long ProductId, string ProductName, long AuthorUserId, string AuthorDisplayName,
    int Rating, string Text, CriteriaDto? Criteria);

/// <param name="Version">Версія, яку клієнт бачив востаннє (оптимістична конкурентність). Необов'язкова.</param>
public sealed record UpdateReviewRequest(int Rating, string Text, CriteriaDto? Criteria, int? Version);

public sealed record AddCommentRequest(long AuthorId, string Text);
