using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.Dtos;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class CreateCategoryDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";
}

public class CategoryWithPropertiesDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<PropertyDto> Properties { get; set; } = new();
}
