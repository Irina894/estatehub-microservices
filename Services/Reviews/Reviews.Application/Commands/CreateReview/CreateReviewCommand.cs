using Reviews.Application.Caching;
using Reviews.Application.Common.Messaging;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Commands.CreateReview;

public sealed record CreateReviewCommand(
    long ProductId,
    string ProductName,
    long AuthorUserId,
    string AuthorDisplayName,
    int Rating,
    string Text,
    CriteriaDto? Criteria) : ICommand<ReviewDto>, ICacheInvalidating
{
    // новий відгук потрапляє у списки; окремого ключа ще немає
    public IEnumerable<string> KeysToRemove => [];
    public bool InvalidatesLists => true;
    public string Reason => "ReviewCreated";
}
