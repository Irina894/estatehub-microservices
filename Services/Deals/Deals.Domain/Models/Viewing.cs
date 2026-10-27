namespace Deals.Domain.Models;

public class Viewing : EntityBase
{
    public Guid ApplicationId { get; set; }
    public Guid RealtorId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = "scheduled";
}
