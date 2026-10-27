namespace Catalog.Dal.Repositories;

public class UnitOfWork(CatalogDbContext context) : IUnitOfWork
{
    private IPropertyRepository? _properties;
    private ICategoryRepository? _categories;
    private IAmenityRepository? _amenities;
    private IPropertyAmenityRepository? _propertyAmenities;

    // Усі репозиторії ділять ОДИН DbContext -> один SaveChangesAsync = одна транзакція
    public IPropertyRepository Properties => _properties ??= new PropertyRepository(context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(context);
    public IAmenityRepository Amenities => _amenities ??= new AmenityRepository(context);
    public IPropertyAmenityRepository PropertyAmenities => _propertyAmenities ??= new PropertyAmenityRepository(context);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
