namespace Catalog.Dal.Repositories;

public interface IUnitOfWork
{
    IPropertyRepository Properties { get; }
    ICategoryRepository Categories { get; }
    IAmenityRepository Amenities { get; }
    IPropertyAmenityRepository PropertyAmenities { get; }

    /// <summary>Єдина точка збереження: усі зміни бізнес-операції — однією транзакцією.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
