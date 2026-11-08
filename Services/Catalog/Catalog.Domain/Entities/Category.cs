using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

/// <summary>Категорія (тип) нерухомості: Квартира, Будинок, Офіс. Зв'язок 1:N з Property.</summary>
public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }

    public List<Property> Properties { get; } = new();

    private Category() { }                       // для EF Core

    public Category(string name)
    {
        Id = Guid.NewGuid();
        Rename(name);
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required");
        if (name.Trim().Length > 100)
            throw new DomainException("Category name must be at most 100 characters");
        Name = name.Trim();
    }
}
