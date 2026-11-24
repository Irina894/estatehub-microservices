using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Dal.Repositories;

public interface IPropertyRepository : IGenericRepository<Property>
{
    /// <summary>EAGER LOADING: Property + Category + Location + Images + Amenities одним запитом.</summary>
    Task<Property?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Property>> ListAsync(ISpecification<Property> spec, CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<Property> spec, CancellationToken ct = default);
    Task<bool> ExistsByOwnerAndTitleAsync(Guid ownerId, string title, Guid? excludeId = null, CancellationToken ct = default);
}
