using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties", t =>
            t.HasCheckConstraint("CK_Property_Price", "[Price] > 0"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(4000);
        builder.Property(p => p.City).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");

        // enum <-> рядок у нижньому регістрі ("available")
        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<PropertyStatus>(v, true));

        builder.Property(p => p.RowVersion).IsRowVersion();                  // оптимістична конкурентність
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).IsRequired();

        // М'яке видалення: видалені об'єкти не потрапляють у жоден запит
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Індекси під конкретні запити
        builder.HasIndex(p => new { p.City, p.Price }).HasDatabaseName("IX_Properties_City_Price");
        builder.HasIndex(p => p.Status).HasDatabaseName("IX_Properties_Status").HasFilter("[Status] = 'available'");
        builder.HasIndex(p => p.CategoryId).HasDatabaseName("IX_Properties_CategoryId");

        // Category 1:N Property (RESTRICT: категорію з об'єктами видалити не можна)
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Properties)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:1 Property <-> PropertyLocation (унікальний FK PropertyId створюється автоматично)
        builder.HasOne(p => p.Location)
            .WithOne()
            .HasForeignKey<PropertyLocation>(l => l.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:N Property -> PropertyImage
        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
