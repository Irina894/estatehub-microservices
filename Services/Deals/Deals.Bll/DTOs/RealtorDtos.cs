using System.ComponentModel.DataAnnotations;

namespace Deals.Bll.DTOs;

public class CreateRealtorDto
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;
}

public class UpdateRealtorDto : CreateRealtorDto
{
    public bool IsActive { get; set; } = true;
}

public class RealtorDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
