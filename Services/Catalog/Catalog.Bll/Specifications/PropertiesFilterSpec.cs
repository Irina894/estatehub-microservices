using Ardalis.Specification;
using Catalog.Bll.Dtos;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;

namespace Catalog.Bll.Specifications;

/// <summary>Лише фільтри (WHERE). Використовується і для COUNT, і як база для сторінки.</summary>
public class PropertiesFilterSpec : Specification<Property>
{
    public PropertiesFilterSpec(PropertyQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            Query.Where(p => p.Title.Contains(term) || p.City.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(q.City))
        {
            var city = q.City.Trim();
            Query.Where(p => p.City == city);
        }

        if (q.CategoryId.HasValue)
        {
            var categoryId = q.CategoryId.Value;
            Query.Where(p => p.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(q.Status) && Enum.TryParse<PropertyStatus>(q.Status, true, out var status))
            Query.Where(p => p.Status == status);

        if (q.MinPrice.HasValue)
        {
            var min = q.MinPrice.Value;
            Query.Where(p => p.Price >= min);
        }

        if (q.MaxPrice.HasValue)
        {
            var max = q.MaxPrice.Value;
            Query.Where(p => p.Price <= max);
        }
    }
}
