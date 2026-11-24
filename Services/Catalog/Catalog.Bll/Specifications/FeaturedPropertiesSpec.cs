using Ardalis.Specification;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;

namespace Catalog.Bll.Specifications;

/// <summary>Друга специфікація: вільні об'єкти міста, від найдешевших.</summary>
public class FeaturedPropertiesSpec : Specification<Property>
{
    public FeaturedPropertiesSpec(string city, int limit)
    {
        Query
            .Where(p => p.City == city && p.Status == PropertyStatus.Available)
            .Include(p => p.Category)
            .OrderBy(p => p.Price)
            .ThenBy(p => p.Id)
            .Take(limit)
            .AsNoTracking();
    }
}
