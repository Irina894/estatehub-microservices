using System.Net;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Clients;

/// <summary>Типізований клієнт сервісу Deals (замовлення/заявки).</summary>
public class DealsClient(HttpClient http)
{
    public async Task<ApplicationDto?> GetApplicationAsync(Guid id, CancellationToken ct)
        => await HttpClientExtensions.GetOrNullAsync<ApplicationDto>(http, $"/api/applications/{id}", ct);
}

/// <summary>Типізований клієнт сервісу Catalog.</summary>
public class CatalogClient(HttpClient http)
{
    public async Task<PropertyDto?> GetPropertyAsync(Guid id, CancellationToken ct)
        => await HttpClientExtensions.GetOrNullAsync<PropertyDto>(http, $"/api/properties/{id}", ct);
}

/// <summary>Типізований клієнт сервісу Reviews.</summary>
public class ReviewsClient(HttpClient http)
{
    public async Task<IReadOnlyList<ReviewDto>> GetForProductAsync(long productId, CancellationToken ct)
    {
        var page = await HttpClientExtensions.GetOrNullAsync<PagedReviewsDto>(
            http, $"/api/reviews?productId={productId}&pageSize=50", ct);
        return page?.Items ?? [];
    }
}

internal static class HttpClientExtensions
{
    /// <summary>GET з десеріалізацією; 404 -> null, інші помилки -> HttpRequestException.</summary>
    public static async Task<T?> GetOrNullAsync<T>(HttpClient http, string url, CancellationToken ct) where T : class
    {
        using var response = await http.GetAsync(url, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }
}
