using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

/// <summary>Оцінки за критеріями (як у схемі Лаб. 1: location / condition / price), кожна 1..5.</summary>
public sealed class Criteria : ValueObject
{
    public int Location { get; }
    public int Condition { get; }
    public int Price { get; }

    public Criteria(int location, int condition, int price)
    {
        Check(location, nameof(location));
        Check(condition, nameof(condition));
        Check(price, nameof(price));
        Location = location;
        Condition = condition;
        Price = price;
    }

    private static void Check(int v, string name)
    {
        if (v < 1 || v > 5) throw new DomainException($"Оцінка '{name}' має бути в діапазоні 1..5.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Location;
        yield return Condition;
        yield return Price;
    }
}
