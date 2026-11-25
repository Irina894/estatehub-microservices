using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public class AmenityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class CreateAmenityDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";
}
