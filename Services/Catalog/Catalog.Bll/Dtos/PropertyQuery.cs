namespace Catalog.Bll.Dtos;

/// <summary>Параметри списку: фільтри, сортування, пагінація (query string).</summary>
public class PropertyQuery
{
    public const int MaxPageSize = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }
    public string? City { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Status { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    /// <summary>title | price | city | createdAt</summary>
    public string SortBy { get; set; } = "createdAt";
    /// <summary>asc | desc</summary>
    public string SortDir { get; set; } = "desc";

    // Безпечні значення: pageSize обмежується 1..100, page >= 1
    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => Math.Clamp(PageSize, 1, MaxPageSize);
}
