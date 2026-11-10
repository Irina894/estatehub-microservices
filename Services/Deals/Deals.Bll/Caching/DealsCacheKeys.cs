namespace Deals.Bll.Caching;

/// <summary>Схема імен ключів Deals: deals:&lt;сутність&gt;:&lt;id&gt;. Префікс ізолює їх від інших сервісів у спільному Redis.</summary>
public static class DealsCacheKeys
{
    public static string Application(Guid id) => $"deals:application:{id}";
    public static string Realtor(Guid id) => $"deals:realtor:{id}";
    public const string RealtorsAll = "deals:realtors:all";
}
