using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Dal;

/// <summary>
/// Design-time фабрика: dotnet ef створює DbContext через неї і не запускає Program.cs
/// (тому під час `migrations add` не виконуються MigrateAsync та сідінг).
/// Рядок підключення: змінна оточення CATALOG_DB або значення за замовчуванням нижче.
/// </summary>
public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    private const string DefaultConnection =
        "Server=(localdb)\\mssqllocaldb;Database=EstateHub_Catalog_EF;AttachDbFileName=C:\\Users\\Public\\EstateHub_Catalog_EF.mdf;Trusted_Connection=True;TrustServerCertificate=True";

    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("CATALOG_DB") ?? DefaultConnection;
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer(connection)
            .Options;
        return new CatalogDbContext(options);
    }
}
