using EstateHub.Catalog.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EstateHub.Catalog.Data
{
    public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
    {
        public CatalogDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
            optionsBuilder.UseSqlServer(
               "Server=(localdb)\\MSSQLLocalDB;Database=EstateHub_Catalog;Trusted_Connection=True;TrustServerCertificate=True");

            return new CatalogDbContext(optionsBuilder.Options);
        }
    }
}