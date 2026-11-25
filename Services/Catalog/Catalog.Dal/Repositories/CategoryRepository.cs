using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class CategoryRepository(CatalogDbContext context) : GenericRepository<Category>(context), ICategoryRepository
{
    // Explicit Loading: спочатку Category (запит №1), потім колекція через Entry().Collection().Query().LoadAsync() (запит №2)
    public async Task<Category?> GetWithPropertiesAsync(Guid id, CancellationToken ct = default)
    {
        var category = await Set.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return null;

        await Context.Entry(category)
            .Collection(c => c.Properties)
            .Query()
            .OrderBy(p => p.Title)
            .LoadAsync(ct);

        return category;
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await Set.AnyAsync(c => c.Name == name, ct);
}
