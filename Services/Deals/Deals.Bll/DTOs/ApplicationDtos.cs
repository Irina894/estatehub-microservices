using System.ComponentModel.DataAnnotations;

namespace Deals.Bll.DTOs;

// ---- рівень 1 валідації: DataAnnotations на DTO (перевіряються [ApiController] автоматично -> 400) ----

public class CreateApplicationDto
{
    public Guid ClientId { get; set; }
    public Guid PropertyId { get; set; }

    /// <summary>Знімок ціни об'єкта з Каталогу на момент подання заявки.</summary>
    [Range(0.01, 1_000_000_000_000)]
    public decimal PriceSnapshot { get; set; }

    /// <summary>Знімок назви об'єкта з Каталогу на момент подання заявки.</summary>
    [Required, StringLength(200, MinimumLength = 1)]
    public string PropertyTitleSnapshot { get; set; } = string.Empty;
}

public class ChangeApplicationStatusDto
{
    [Required, StringLength(30)]
    public string Status { get; set; } = string.Empty;

    /// <summary>Версія рядка, отримана клієнтом разом із заявкою (оптимістична конкурентність).</summary>
    [Required, MinLength(1)]
    public byte[] RowVersion { get; set; } = [];
}

public class ApproveApplicationDto
{
    [Required, MinLength(1)]
    public byte[] RowVersion { get; set; } = [];
    public Guid RealtorId { get; set; }
    public DateTime ViewingDate { get; set; }
}

public class AssignRealtorDto
{
    public Guid RealtorId { get; set; }

    [Required, RegularExpression("^(listing_agent|buyer_agent)$",
        ErrorMessage = "Role must be 'listing_agent' or 'buyer_agent'.")]
    public string Role { get; set; } = string.Empty;
}

public class SignContractDto
{
    [Required, MinLength(1)]
    public byte[] RowVersion { get; set; } = [];

    [Range(0.01, 1_000_000_000_000)]
    public decimal FinalPrice { get; set; }
}

public class ApplicationDto
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal PriceSnapshot { get; set; }
    public string PropertyTitleSnapshot { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public byte[]? RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ViewingDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid RealtorId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ContractDto
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public decimal FinalPrice { get; set; }
    public DateTime SignedAt { get; set; }
}

public class ApplicationRealtorDto
{
    public Guid ApplicationId { get; set; }
    public Guid RealtorId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
