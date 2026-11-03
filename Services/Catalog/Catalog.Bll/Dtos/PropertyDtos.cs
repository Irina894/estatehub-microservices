using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public class PropertyDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public string City { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class LocationDto
{
    public string FullAddress { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsGeocoded { get; set; }
}

public class ImageDto
{
    public string ImageUrl { get; set; } = "";
    public int SortOrder { get; set; }
}

public class PropertyDetailsDto : PropertyDto
{
    public string Description { get; set; } = "";
    public Guid OwnerId { get; set; }
    public LocationDto? Location { get; set; }
    public List<ImageDto> Images { get; set; } = new();
    public List<AmenityDto> Amenities { get; set; } = new();
}

public class CreateLocationDto
{
    [Required, StringLength(500, MinimumLength = 3)]
    public string FullAddress { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class CreatePropertyDto
{
    [Required] public Guid CategoryId { get; set; }
    [Required] public Guid OwnerId { get; set; }

    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = "";

    [Required, StringLength(4000)]
    public string Description { get; set; } = "";

    [Range(0.01, 1_000_000_000)]
    public decimal Price { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = "";

    public CreateLocationDto? Location { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

public class UpdatePropertyDto
{
    [Required] public Guid CategoryId { get; set; }

    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = "";

    [Required, StringLength(4000)]
    public string Description { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = "";
}

public class ChangePriceDto
{
    [Range(0.01, 1_000_000_000)]
    public decimal Price { get; set; }
}

public class ChangeStatusDto
{
    /// <summary>available | reserved | sold | rented</summary>
    [Required]
    public string Status { get; set; } = "";
}
