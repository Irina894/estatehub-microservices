using Ardalis.Specification;
using Catalog.Bll.Dtos;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

/// <summary>Фільтри + Include + сортування (білий список) + Skip/Take. Усе перетворюється на один SQL.</summary>
public class PropertiesPageSpec : PropertiesFilterSpec
{
    public PropertiesPageSpec(PropertyQuery q) : base(q)
    {
        var desc = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        IOrderedSpecificationBuilder<Property> ordered = (q.SortBy?.ToLowerInvariant(), desc) switch
        {
            ("title", true) => Query.OrderByDescending(p => p.Title),
            ("title", false) => Query.OrderBy(p => p.Title),
            ("price", true) => Query.OrderByDescending(p => p.Price),
            ("price", false) => Query.OrderBy(p => p.Price),
            ("city", true) => Query.OrderByDescending(p => p.City),
            ("city", false) => Query.OrderBy(p => p.City),
            (_, true) => Query.OrderByDescending(p => p.CreatedAt),
            _ => Query.OrderBy(p => p.CreatedAt)
        };

        // другий ключ робить порядок детермінованим (інакше сторінки можуть «плавати»)
        ordered.ThenBy(p => p.Id);

        Query
            .Include(p => p.Category)
            .Skip((q.NormalizedPage - 1) * q.NormalizedPageSize)
            .Take(q.NormalizedPageSize)
            .AsNoTracking();
    }
}
