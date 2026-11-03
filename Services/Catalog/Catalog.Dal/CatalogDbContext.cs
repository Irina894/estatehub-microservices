using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<PropertyLocation> PropertyLocations => Set<PropertyLocation>();
    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();

    // Усі IEntityTypeConfiguration<T> з цієї збірки підключаються централізовано
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
}
