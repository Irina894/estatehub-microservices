using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>
/// Проміжна сутність M:N між Property і Amenity із власним полем AddedAt (коли зручність додано до об'єкта).
/// Складений ключ (PropertyId, AmenityId) налаштовано у PropertyAmenityConfiguration.
/// </summary>
public class PropertyAmenity
{
    public Guid PropertyId { get; private set; }
    public Property Property { get; private set; } = null!;
    public Guid AmenityId { get; private set; }
    public Amenity Amenity { get; private set; } = null!;
    public DateTime AddedAt { get; private set; }

    private PropertyAmenity() { }

    public PropertyAmenity(Guid propertyId, Guid amenityId)
    {
        if (propertyId == Guid.Empty || amenityId == Guid.Empty)
            throw new DomainException("PropertyId and AmenityId are required");
        PropertyId = propertyId;
        AmenityId = amenityId;
        AddedAt = DateTime.UtcNow;
    }
}
