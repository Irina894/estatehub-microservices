
using EstateHub.Catalog.Data;
using EstateHub.Catalog.Models;
using Microsoft.EntityFrameworkCore;
Console.OutputEncoding = System.Text.Encoding.UTF8;
var connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=EstateHub_Catalog;Trusted_Connection=True;TrustServerCertificate=True";

var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
optionsBuilder.UseSqlServer(connectionString);

using var db = new CatalogDbContext(optionsBuilder.Options);

var category = await db.Categories.FirstAsync();

var testProperty = new Property
{
    CategoryId = category.Id,
    OwnerId = Guid.NewGuid(),
    Title = "Тест soft delete",
    Description = "Тимчасовий запис для перевірки HasQueryFilter",
    Price = 100000,
    City = "Київ"
};

db.Properties.Add(testProperty);
await db.SaveChangesAsync();
Console.WriteLine($"Створено тестовий Property: {testProperty.Id}");

testProperty.IsDeleted = true;
await db.SaveChangesAsync();
Console.WriteLine("IsDeleted = true, зберегли зміни.");

var foundWithFilter = await db.Properties.FirstOrDefaultAsync(p => p.Id == testProperty.Id);
Console.WriteLine(foundWithFilter is null
    ? "OK: HasQueryFilter працює — EF не бачить видалений рядок у звичайному запиті."
    : "ПОМИЛКА: фільтр не спрацював, рядок все ще повертається.");

var foundIgnoringFilter = await db.Properties
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(p => p.Id == testProperty.Id);
Console.WriteLine(foundIgnoringFilter is not null
    ? "OK: рядок фізично існує в базі (IgnoreQueryFilters його бачить)."
    : "ПОМИЛКА: рядок видалився фізично — це вже не soft delete.");

db.Properties.Remove(foundIgnoringFilter!);
await db.SaveChangesAsync();
Console.WriteLine("Тестовий запис прибрано.");