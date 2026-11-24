using Microsoft.Extensions.Caching.Memory;

namespace Reviews.Application.Caching;

/// <summary>Схема імен ключів Reviews: reviews:&lt;...&gt;. Префікс ізолює їх від інших сервісів у спільному Redis.</summary>
public static class ReviewsCacheKeys
{
    public const string ListScope = "reviews:list";

    public static string Review(string id) => $"reviews:review:{id}";

    public static string List(string version, long? productId, int? minRating, string? search, int page, int pageSize)
    {
        // пошуковий рядок довільний, тому беремо короткий хеш від нього
        var searchHash = string.IsNullOrWhiteSpace(search)
            ? "-"
            : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(search.Trim().ToLowerInvariant())))[..12];
        return $"{ListScope}:v{version}:p{productId?.ToString() ?? "-"}:r{minRating?.ToString() ?? "-"}:s{searchHash}:{page}:{pageSize}";
    }
}

/// <summary>Запит, відповідь на який кешується (L1 → Redis → MongoDB). Реалізують самі запити CQRS.</summary>
public interface ICachedQuery
{
    /// <summary>true для списків: у ключ входить версія групи, і зміна даних інвалідує всі списки одразу.</summary>
    bool UsesListVersion { get; }
    string BuildKey(string listVersion);
    CacheTtl Ttl { get; }
}

/// <summary>Команда, після успішного виконання якої треба інвалідувати кеш.</summary>
public interface ICacheInvalidating
{
    /// <summary>Ключі окремих відгуків, які стали застарілими.</summary>
    IEnumerable<string> KeysToRemove { get; }

    /// <summary>true, якщо зміна може вплинути на списки.</summary>
    bool InvalidatesLists { get; }

    string Reason { get; }
}
