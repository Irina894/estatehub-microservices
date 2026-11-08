namespace Platform.Aggregator.Models;

// ---- Моделі відповідей окремих сервісів (лише потрібні поля; решта JSON ігнорується) ----

/// <summary>Заявка з Deals (Лаб. 2).</summary>
public record ApplicationDto(
    Guid Id,
    Guid ClientId,
    Guid PropertyId,
    decimal PriceSnapshot,
    string PropertyTitleSnapshot,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Об'єкт нерухомості з Catalog (Лаб. 3).</summary>
public record PropertyDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string City,
    string Status,
    string CategoryName,
    LocationDto? Location);

public record LocationDto(string FullAddress, double? Latitude, double? Longitude);

/// <summary>Елемент списку відгуків з Reviews (Лаб. 4).</summary>
public record ReviewDto(
    string Id,
    long ProductId,
    string ProductName,
    string AuthorName,
    int Rating,
    string Text,
    int CommentsCount,
    DateTime CreatedAt);

public record PagedReviewsDto(IReadOnlyList<ReviewDto> Items, long TotalCount, int Page, int PageSize);

// ---- Агрегована відповідь: структура відображає бізнес-сценарій «картка заявки» ----

/// <summary>Картка заявки: заявка + об'єкт + відгуки на нього + попередження про недоступні сервіси.</summary>
public record FullApplicationDto(
    ApplicationDto Application,
    PropertyDto? Property,
    IReadOnlyList<ReviewDto> Reviews,
    double? AverageRating,
    IReadOnlyList<string> Warnings);
