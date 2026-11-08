using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

/// <summary>Автор відгуку (вбудований документ author у MongoDB).</summary>
public sealed class Author : ValueObject
{
    public long UserId { get; }
    public string DisplayName { get; }

    public Author(long userId, string displayName)
    {
        if (userId <= 0) throw new DomainException("UserId автора має бути додатним.");
        if (string.IsNullOrWhiteSpace(displayName)) throw new DomainException("Ім'я автора не може бути порожнім.");
        UserId = userId;
        DisplayName = displayName.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return DisplayName;
    }
}
