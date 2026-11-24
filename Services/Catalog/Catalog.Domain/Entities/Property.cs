using Catalog.Domain.Enums;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>
/// Агрегат-корінь каталогу. Усі бізнес-правила — у методах сутності, а не у відкритих setter-ах
/// (аналог CatalogItem з Microsoft eShop: RemoveStock / AddStock).
/// </summary>
public class Property
{
    private static readonly Dictionary<PropertyStatus, PropertyStatus[]> AllowedTransitions = new()
    {
        [PropertyStatus.Available] = new[] { PropertyStatus.Reserved, PropertyStatus.Sold, PropertyStatus.Rented },
        [PropertyStatus.Reserved]  = new[] { PropertyStatus.Available, PropertyStatus.Sold, PropertyStatus.Rented },
        [PropertyStatus.Rented]    = new[] { PropertyStatus.Available },
        [PropertyStatus.Sold]      = Array.Empty<PropertyStatus>()
    };

    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public Guid OwnerId { get; private set; }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public decimal Price { get; private set; }
    public string City { get; private set; } = "";
    public PropertyStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();   // оптимістична конкурентність
    public bool IsDeleted { get; private set; }                             // м'яке видалення
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public PropertyLocation? Location { get; private set; }                  // 1:1
    public List<PropertyImage> Images { get; } = new();                      // 1:N
    public List<PropertyAmenity> PropertyAmenities { get; } = new();         // M:N через проміжну сутність

    private Property() { }                                                   // для EF Core

    public Property(Guid categoryId, Guid ownerId, string title, string description, decimal price, string city)
    {
        if (categoryId == Guid.Empty) throw new DomainException("Category is required");
        if (ownerId == Guid.Empty) throw new DomainException("Owner is required");

        Id = Guid.NewGuid();
        CategoryId = categoryId;
        OwnerId = ownerId;
        Title = ValidateTitle(title);
        Description = ValidateDescription(description);
        City = ValidateCity(city);
        Price = ValidatePrice(price);
        Status = PropertyStatus.Available;
        CreatedAt = DateTime.UtcNow;
    }

    // ---------- бізнес-операції ----------

    public void ChangePrice(decimal newPrice)
    {
        Price = ValidatePrice(newPrice);
        Touch();
    }

    public void UpdateDetails(Guid categoryId, string title, string description, string city)
    {
        if (categoryId == Guid.Empty) throw new DomainException("Category is required");
        CategoryId = categoryId;
        Title = ValidateTitle(title);
        Description = ValidateDescription(description);
        City = ValidateCity(city);
        Touch();
    }

    public void ChangeStatus(PropertyStatus newStatus)
    {
        if (newStatus == Status)
            throw new DomainException($"Property is already '{Status}'");
        if (!AllowedTransitions[Status].Contains(newStatus))
            throw new DomainException($"Cannot change status from '{Status}' to '{newStatus}'");
        Status = newStatus;
        Touch();
    }

    public void SoftDelete()
    {
        if (IsDeleted) throw new DomainException("Property is already deleted");
        IsDeleted = true;
        Touch();
    }

    public void SetLocation(string fullAddress, double? latitude = null, double? longitude = null)
    {
        Location = new PropertyLocation(Id, fullAddress, latitude, longitude);
        Touch();
    }

    public void AddImage(string imageUrl)
    {
        Images.Add(new PropertyImage(Id, imageUrl, Images.Count));
        Touch();
    }

    /// <summary>Додає зручність. Колекцію PropertyAmenities потрібно завантажити (Include) перед викликом.</summary>
    public void AddAmenity(Guid amenityId)
    {
        if (PropertyAmenities.Any(pa => pa.AmenityId == amenityId))
            throw new DomainException("Amenity is already assigned to this property");
        PropertyAmenities.Add(new PropertyAmenity(Id, amenityId));
        Touch();
    }

    // ---------- допоміжне ----------

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private static decimal ValidatePrice(decimal price)
    {
        if (price <= 0) throw new DomainException("Price must be positive");
        if (decimal.Round(price, 2) != price) throw new DomainException("Price must have at most 2 decimal places");
        return price;
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Title is required");
        if (title.Trim().Length > 200) throw new DomainException("Title must be at most 200 characters");
        return title.Trim();
    }

    private static string ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new DomainException("Description is required");
        if (description.Length > 4000) throw new DomainException("Description must be at most 4000 characters");
        return description.Trim();
    }

    private static string ValidateCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city)) throw new DomainException("City is required");
        if (city.Trim().Length > 100) throw new DomainException("City must be at most 100 characters");
        return city.Trim();
    }
}
