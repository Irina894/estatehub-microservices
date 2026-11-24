using Reviews.Application.Caching;
using Reviews.Application.Common.Messaging;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Commands.UpdateReview;

/// <param name="ExpectedVersion">Версія, яку клієнт бачив востаннє. Якщо не збігається з БД — 409.</param>
public sealed record UpdateReviewCommand(
    string Id,
    int Rating,
    string Text,
    CriteriaDto? Criteria,
    int? ExpectedVersion) : ICommand<ReviewDto>, ICacheInvalidating
{
    public IEnumerable<string> KeysToRemove => [ReviewsCacheKeys.Review(Id)];
    public bool InvalidatesLists => true;   // у списках видно рейтинг і текст
    public string Reason => "ReviewUpdated";
}
