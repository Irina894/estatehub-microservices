using System.Security.Cryptography;
using System.Text;
using Catalog.Bll.Dtos;

namespace Catalog.Bll.Caching;

/// <summary>
/// Схема імен ключів: catalog:&lt;сутність&gt;:&lt;...&gt;. Префікс "catalog:" ізолює ключі від інших сервісів у спільному Redis.
/// Для списків у ключ входить версія групи (<see cref="PropertiesScope"/>): зміна даних збільшує версію,
/// і всі старі списки одним кроком перестають використовуватися (масова інвалідація без перебору ключів).
/// </summary>
public static class CatalogCacheKeys
{
    public const string PropertiesScope = "catalog:properties";

    public static string Property(Guid id) => $"catalog:property:{id}";
    public static string CategoriesAll => "catalog:categories:all";
    public static string Category(Guid id) => $"catalog:category:{id}";

    public static string PropertiesList(string version, PropertyQuery q)
    {
        // фільтри можуть бути довгими, тому беремо короткий хеш від нормалізованого запису
        var raw = string.Join('|',
            q.NormalizedPage, q.NormalizedPageSize,
            q.Search?.Trim().ToLowerInvariant(), q.City?.Trim().ToLowerInvariant(),
            q.CategoryId, q.Status?.Trim().ToLowerInvariant(), q.MinPrice, q.MaxPrice,
            q.SortBy?.ToLowerInvariant(), q.SortDir?.ToLowerInvariant());
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
        return $"{PropertiesScope}:list:v{version}:{q.NormalizedPage}:{q.NormalizedPageSize}:{hash}";
    }

    public static string Featured(string version, string city, int limit) =>
        $"{PropertiesScope}:featured:v{version}:{city.Trim().ToLowerInvariant()}:{limit}";
}
