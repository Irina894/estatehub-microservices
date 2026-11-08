using Catalog.Dal.Seeding;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName("UQ_Categories_Name");

        // Статичний сідінг довідника з фіксованими Id (анонімні об'єкти — бо сеттери приватні)
        builder.HasData(
            new { Id = CatalogSeedIds.CategoryOffice, Name = "Офіс", CreatedAt = CatalogSeedIds.SeedDate },
            new { Id = CatalogSeedIds.CategoryHouse, Name = "Будинок", CreatedAt = CatalogSeedIds.SeedDate },
            new { Id = CatalogSeedIds.CategoryApartment, Name = "Квартира", CreatedAt = CatalogSeedIds.SeedDate });
    }
}
