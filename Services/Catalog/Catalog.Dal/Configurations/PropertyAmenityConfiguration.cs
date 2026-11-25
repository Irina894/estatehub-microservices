using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

/// <summary>M:N Property <-> Amenity через явну проміжну сутність із власним полем AddedAt.</summary>
public class PropertyAmenityConfiguration : IEntityTypeConfiguration<PropertyAmenity>
{
    public void Configure(EntityTypeBuilder<PropertyAmenity> builder)
    {
        builder.ToTable("PropertyAmenities");

        builder.HasKey(pa => new { pa.PropertyId, pa.AmenityId });           // складений ключ
        builder.Property(pa => pa.AddedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(pa => pa.Property)
            .WithMany(p => p.PropertyAmenities)
            .HasForeignKey(pa => pa.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.Amenity)
            .WithMany(a => a.PropertyAmenities)
            .HasForeignKey(pa => pa.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);

        // під запит "об'єкти зі зручністю X, додані після дати"
        builder.HasIndex(pa => new { pa.AmenityId, pa.AddedAt }).HasDatabaseName("IX_PropertyAmenities_AmenityId_AddedAt");
    }
}
