using Catalog.Bll.Dtos;
using Catalog.Bll.Specifications;
using Catalog.Bll.Validation;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.Exceptions;
using Xunit;

namespace Catalog.Tests;

public class PropertyDomainTests
{
    private static Property Make(string title = "Квартира", decimal price = 1000m, string city = "Київ")
        => new(Guid.NewGuid(), Guid.NewGuid(), title, "Опис", price, city);

    [Fact]
    public void Ctor_ZeroPrice_Throws() => Assert.Throws<DomainException>(() => Make(price: 0));

    [Fact]
    public void ChangeStatus_Available_To_Reserved_Ok()
    {
        var p = Make();
        p.ChangeStatus(PropertyStatus.Reserved);
        Assert.Equal(PropertyStatus.Reserved, p.Status);
    }

    [Fact]
    public void ChangeStatus_Sold_To_Available_Throws()
    {
        var p = Make();
        p.ChangeStatus(PropertyStatus.Sold);
        Assert.Throws<DomainException>(() => p.ChangeStatus(PropertyStatus.Available));
    }

    [Fact]
    public void AddAmenity_Duplicate_Throws()
    {
        var p = Make();
        var id = Guid.NewGuid();
        p.AddAmenity(id);
        Assert.Throws<DomainException>(() => p.AddAmenity(id));
    }

    [Fact]
    public void SoftDelete_SetsFlag()
    {
        var p = Make();
        p.SoftDelete();
        Assert.True(p.IsDeleted);
    }
}

public class SpecificationTests
{
    private static List<Property> Data()
    {
        var cat = Guid.NewGuid();
        var owner = Guid.NewGuid();
        return new()
        {
            new Property(cat, owner, "A", "d", 300m, "Київ"),
            new Property(cat, owner, "B", "d", 100m, "Київ"),
            new Property(cat, owner, "C", "d", 200m, "Львів"),
            new Property(cat, owner, "D", "d", 400m, "Київ"),
        };
    }

    [Fact]
    public void FilterSpec_ByCityAndPrice()
    {
        var q = new PropertyQuery { City = "Київ", MinPrice = 150m };
        var result = new PropertiesFilterSpec(q).Evaluate(Data()).ToList();
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal("Київ", p.City));
    }

    [Fact]
    public void PageSpec_SortsByPrice_AndPages()
    {
        var q = new PropertyQuery { SortBy = "price", SortDir = "asc", Page = 2, PageSize = 2 };
        var result = new PropertiesPageSpec(q).Evaluate(Data()).ToList();
        Assert.Equal(new[] { 300m, 400m }, result.Select(p => p.Price).ToArray());
    }
}

public class ValidationAndPagingTests
{
    [Fact]
    public void PageSize_IsClamped()
    {
        Assert.Equal(100, new PropertyQuery { PageSize = 5000 }.NormalizedPageSize);
        Assert.Equal(1, new PropertyQuery { PageSize = 0 }.NormalizedPageSize);
    }

    [Fact]
    public void QueryValidator_MinGreaterThanMax_Fails()
    {
        var r = new PropertyQueryValidator().Validate(new PropertyQuery { MinPrice = 10, MaxPrice = 5 });
        Assert.False(r.IsValid);
    }

    [Fact]
    public void StatusValidator_UnknownStatus_Fails()
        => Assert.False(new ChangeStatusDtoValidator().Validate(new ChangeStatusDto { Status = "foo" }).IsValid);

    [Fact]
    public void PagedResult_Flags()
    {
        var r = new PagedResult<int>(new[] { 1, 2 }, 5, 2, 2);
        Assert.Equal(3, r.TotalPages);
        Assert.True(r.HasNext);
        Assert.True(r.HasPrevious);
    }
}
