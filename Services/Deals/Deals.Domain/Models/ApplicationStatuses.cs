namespace Deals.Domain.Models;

/// <summary>Життєвий цикл заявки: new -> approved -> completed; rejected - з new/approved.</summary>
public static class ApplicationStatuses
{
    public const string New = "new";
    public const string Approved = "approved";
    public const string Completed = "completed";
    public const string Rejected = "rejected";

    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        [New] = [Approved, Rejected],
        [Approved] = [Completed, Rejected],
        [Completed] = [],
        [Rejected] = []
    };

    public static bool IsKnown(string status) => Allowed.ContainsKey(status);

    public static bool CanTransition(string from, string to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);
}
