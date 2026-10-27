using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class PropertyLocationConfiguration : IEntityTypeConfiguration<PropertyLocation>
{
    public void Configure(EntityTypeBuilder<PropertyLocation> builder)
    {
        builder.ToTable("PropertyLocations", t =>
        {
            t.HasCheckConstraint("CK_PropertyLocations_Latitude", "[Latitude] IS NULL OR [Latitude] BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_PropertyLocations_Longitude", "[Longitude] IS NULL OR [Longitude] BETWEEN -180 AND 180");
        });

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.FullAddress).IsRequired().HasMaxLength(500);
    }
}
