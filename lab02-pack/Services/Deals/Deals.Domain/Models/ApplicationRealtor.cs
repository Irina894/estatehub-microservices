namespace Deals.Domain.Models;

/// <summary>Junction-таблиця M:N Applications - Realtors (складений ключ, тому не EntityBase).</summary>
public class ApplicationRealtor
{
    public Guid ApplicationId { get; set; }
    public Guid RealtorId { get; set; }
    public string Role { get; set; } = RealtorRoles.ListingAgent;
    public DateTime AssignedAt { get; set; }
}

public static class RealtorRoles
{
    public const string ListingAgent = "listing_agent";
    public const string BuyerAgent = "buyer_agent";
    public static bool IsValid(string role) => role is ListingAgent or BuyerAgent;
}
