namespace Platform.Aggregator.Models;

/// <summary>
/// Тимчасове зіставлення назви об'єкта з числовим productId у сервісі Reviews.
/// Потрібне, бо Reviews (Лаб. 4) ідентифікує об'єкт числом, а Catalog/Deals - Guid.
/// </summary>
public class AggregatorOptions
{
    public const string SectionName = "Aggregator";

    public List<ReviewProductMapping> ReviewProductMap { get; set; } = [];

    /// <summary>Скільки чекаємо відповіді одного сервісу, перш ніж вважати його недоступним.</summary>
    public int ServiceTimeoutSeconds { get; set; } = 3;
}

public class ReviewProductMapping
{
    public string Title { get; set; } = string.Empty;
    public long ProductId { get; set; }
}
