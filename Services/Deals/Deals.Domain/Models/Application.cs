namespace Deals.Domain.Models;

/// <summary>Заявка клієнта на об'єкт. ClientId/PropertyId - логічні посилання на інші сервіси (без FK).</summary>
public class Application : EntityBase
{
    public Guid ClientId { get; set; }
    public Guid PropertyId { get; set; }
    /// <summary>Знімок ціни з Каталогу на момент подання заявки (не оновлюється).</summary>
    public decimal PriceSnapshot { get; set; }
    /// <summary>Знімок назви об'єкта з Каталогу на момент подання заявки (не оновлюється).</summary>
    public string PropertyTitleSnapshot { get; set; } = string.Empty;
    public string Status { get; set; } = ApplicationStatuses.New;
    public byte[]? RowVersion { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
