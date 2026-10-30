using Reviews.Domain.Exceptions;

namespace Reviews.Domain.Entities;

/// <summary>Коментар до відгуку (вбудований у документ Review).</summary>
public sealed class Comment
{
    public const int TextMaxLength = 1000;

    public long AuthorId { get; }
    public string Text { get; }
    public DateTime CreatedAt { get; }

    public Comment(long authorId, string text) : this(authorId, text, DateTime.UtcNow) { }

    public Comment(long authorId, string text, DateTime createdAt)
    {
        if (authorId <= 0) throw new DomainException("AuthorId коментаря має бути додатним.");
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Текст коментаря не може бути порожнім.");
        if (text.Trim().Length > TextMaxLength) throw new DomainException($"Текст коментаря не може перевищувати {TextMaxLength} символів.");
        AuthorId = authorId;
        Text = text.Trim();
        CreatedAt = createdAt;
    }
}
