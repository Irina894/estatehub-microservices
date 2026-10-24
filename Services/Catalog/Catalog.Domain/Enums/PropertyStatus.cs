namespace Catalog.Domain.Enums;

/// <summary>Статус об'єкта нерухомості. У БД зберігається рядком у нижньому регістрі ("available", "reserved", ...).</summary>
public enum PropertyStatus
{
    Available = 0,
    Reserved = 1,
    Sold = 2,
    Rented = 3
}
