using Catalog.Dal.Seeding;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("Amenities");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(a => a.Name).IsUnique().HasDatabaseName("UQ_Amenities_Name");

        builder.HasData(
            new { Id = CatalogSeedIds.AmenityHeating, Name = "Автономне опалення" },
            new { Id = CatalogSeedIds.AmenityParking, Name = "Паркінг" },
            new { Id = CatalogSeedIds.AmenityElevator, Name = "Ліфт" },
            new { Id = CatalogSeedIds.AmenityBalcony, Name = "Балкон" },
            new { Id = CatalogSeedIds.AmenityAirConditioning, Name = "Кондиціонер" });
    }
}
