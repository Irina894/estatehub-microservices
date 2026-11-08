using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>Зручність (Паркінг, Ліфт, ...). Зв'язок M:N з Property через PropertyAmenity.</summary>
public class Amenity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";

    public List<PropertyAmenity> PropertyAmenities { get; } = new();

    private Amenity() { }

    public Amenity(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Amenity name is required");
        if (name.Trim().Length > 100)
            throw new DomainException("Amenity name must be at most 100 characters");
        Id = Guid.NewGuid();
        Name = name.Trim();
    }
}
