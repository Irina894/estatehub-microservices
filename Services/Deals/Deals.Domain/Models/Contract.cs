namespace Deals.Domain.Models;

public class Contract : EntityBase
{
    public Guid ApplicationId { get; set; }
    public decimal FinalPrice { get; set; }
    public DateTime SignedAt { get; set; }
}
