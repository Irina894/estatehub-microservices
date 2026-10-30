using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

/// <summary>Агрегат «Відгук» про об'єкт нерухомості. Інваріанти перевіряються в самій сутності.</summary>
public sealed class Review : BaseEntity
{
    public const int TextMaxLength = 2000;
    public const int MaxComments = 50;

    // не readonly: драйвер MongoDB пропускає readonly-поля під час читання документа
    private List<Comment> _comments = new();

    public long ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public Author Author { get; private set; } = default!;
    public int Rating { get; private set; }
    public string Text { get; private set; } = default!;
    public Criteria? Criteria { get; private set; }
    public IReadOnlyList<Comment> Comments => _comments.AsReadOnly();

    // потрібен для BsonClassMap
    private Review() { }

    public Review(long productId, string productName, Author author, int rating, string text, Criteria? criteria = null)
    {
        if (productId <= 0) throw new DomainException("ProductId має бути додатним.");
        if (string.IsNullOrWhiteSpace(productName)) throw new DomainException("Назва об'єкта не може бути порожньою.");
        ProductId = productId;
        ProductName = productName.Trim();
        Author = author ?? throw new DomainException("Автор обов'язковий.");
        Criteria = criteria;
        SetRating(rating);
        SetText(text);
    }

    public void Update(int rating, string text, Criteria? criteria)
    {
        SetRating(rating);
        SetText(text);
        Criteria = criteria;
        Touch();
    }

    public void AddComment(long authorId, string text)
    {
        if (_comments.Count >= MaxComments)
            throw new DomainException($"Відгук може мати не більше {MaxComments} коментарів.");
        _comments.Add(new Comment(authorId, text));
        Touch();
    }

    private void SetRating(int rating)
    {
        if (rating < 1 || rating > 5) throw new DomainException("Рейтинг має бути в діапазоні 1..5.");
        Rating = rating;
    }

    private void SetText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Текст відгуку не може бути порожнім.");
        if (text.Length > TextMaxLength) throw new DomainException($"Текст відгуку не може перевищувати {TextMaxLength} символів.");
        Text = text.Trim();
    }
}
