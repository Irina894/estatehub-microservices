using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>Фото об'єкта. Зв'язок 1:N (Property -> PropertyImage).</summary>
public class PropertyImage
{
    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string ImageUrl { get; private set; } = "";
    public int SortOrder { get; private set; }

    private PropertyImage() { }

    public PropertyImage(Guid propertyId, string imageUrl, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new DomainException("Image url is required");
        if (imageUrl.Length > 2048)
            throw new DomainException("Image url must be at most 2048 characters");
        Id = Guid.NewGuid();
        PropertyId = propertyId;
        ImageUrl = imageUrl.Trim();
        SortOrder = sortOrder;
    }
}
