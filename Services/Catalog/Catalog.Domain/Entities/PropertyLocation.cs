using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>Розташування об'єкта. Зв'язок 1:1 (Property -> PropertyLocation), унікальний PropertyId.</summary>
public class PropertyLocation
{
    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string FullAddress { get; private set; } = "";
    public bool IsGeocoded { get; private set; }

    private PropertyLocation() { }

    public PropertyLocation(Guid propertyId, string fullAddress, double? latitude = null, double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(fullAddress))
            throw new DomainException("Address is required");
        if (fullAddress.Length > 500)
            throw new DomainException("Address must be at most 500 characters");
        if (latitude is < -90 or > 90)
            throw new DomainException("Latitude must be between -90 and 90");
        if (longitude is < -180 or > 180)
            throw new DomainException("Longitude must be between -180 and 180");
        if ((latitude is null) != (longitude is null))
            throw new DomainException("Latitude and longitude must be provided together");

        Id = Guid.NewGuid();
        PropertyId = propertyId;
        FullAddress = fullAddress.Trim();
        Latitude = latitude;
        Longitude = longitude;
        IsGeocoded = latitude is not null && longitude is not null;
    }
}
