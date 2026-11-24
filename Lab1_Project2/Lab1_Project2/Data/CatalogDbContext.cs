using EstateHub.Catalog.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace EstateHub.Catalog.Data
{
    public class CatalogDbContext : DbContext
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<PropertyLocation> PropertyLocations { get; set; }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<Amenity> Amenities { get; set; }

        public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 1. Налаштування сутності Property
            // ==========================================
            modelBuilder.Entity<Property>(entity =>
            {
                // ОБМЕЖЕННЯ ДОВЖИНИ (запобігає помилкам індексації nvarchar(max))
                entity.Property(p => p.Title).HasMaxLength(200);
                entity.Property(p => p.City).HasMaxLength(100);
                entity.Property(p => p.Status).HasMaxLength(20);
                // Description залишається без обмежень, оскільки не індексується

                // ГЛОБАЛЬНИЙ ФІЛЬТР (Soft Delete)
                entity.HasQueryFilter(p => !p.IsDeleted);

                // ОБМЕЖЕННЯ ТА КОНКУРЕНТНІСТЬ
                // Примітка: синтаксис HasCheckConstraint для EF Core 7+
                entity.ToTable(t => t.HasCheckConstraint("CK_Property_Price", "Price > 0"));
                entity.Property(p => p.RowVersion).IsRowVersion();

                // ІНДЕКСИ
                entity.HasIndex(p => new { p.City, p.Price });
                entity.HasIndex(p => p.Status).HasFilter("[Status] = 'available'");

                // Зв'язок 1:N з Category
                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Properties)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 2. Налаштування Category
            // ==========================================
            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.Name).HasMaxLength(100);
                entity.HasIndex(c => c.Name).IsUnique();
            });

            // ==========================================
            // 3. Налаштування Amenity
            // ==========================================
            modelBuilder.Entity<Amenity>(entity =>
            {
                entity.Property(a => a.Name).HasMaxLength(100);
                entity.HasIndex(a => a.Name).IsUnique();
            });

            // ==========================================
            // 4. Зв'язок 1:1 (Property <-> PropertyLocation)
            // ==========================================
            modelBuilder.Entity<PropertyLocation>(entity =>
            {
                entity.HasOne(pl => pl.Property)
                      .WithOne(p => p.Location)
                      .HasForeignKey<PropertyLocation>(pl => pl.PropertyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 5. Зв'язок 1:N (Property <-> PropertyImages)
            // ==========================================
            modelBuilder.Entity<PropertyImage>(entity =>
            {
                entity.HasOne(pi => pi.Property)
                      .WithMany(p => p.Images)
                      .HasForeignKey(pi => pi.PropertyId)
                      .OnDelete(DeleteBehavior.Cascade); // Тільки тут дозволяємо каскадне видалення
            });

            modelBuilder.Entity<Property>()
     .HasMany(p => p.Amenities)
     .WithMany(a => a.Properties)
     .UsingEntity<Dictionary<string, object>>(
         "PropertyAmenities",
         j => j.HasOne<Amenity>().WithMany().HasForeignKey("AmenityId"),
         j => j.HasOne<Property>().WithMany().HasForeignKey("PropertyId"),
         j =>
         {
             // Власне поле проміжної сутності (вимога лабораторної: M:N з власним полем)
             j.IndexerProperty<DateTime>("AddedAt")
                 .HasDefaultValueSql("GETUTCDATE()");
             j.ToTable("PropertyAmenities");
         }
     );

            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = Guid.Parse("d343467f-5b65-4203-b0e6-5c56d7831411"), Name = "Квартира", CreatedAt = seedDate },
                new Category { Id = Guid.Parse("2700e123-6447-49f3-8b7c-bbddde9531a2"), Name = "Будинок", CreatedAt = seedDate },
                new Category { Id = Guid.Parse("149c4f0b-402a-4bc6-8800-47b2c75a40b8"), Name = "Офіс", CreatedAt = seedDate }
            );

            modelBuilder.Entity<Amenity>().HasData(
                new Amenity { Id = Guid.Parse("c1fa5853-9dcd-4b8c-ab37-18c7ed6bc402"), Name = "Паркінг" },
                new Amenity { Id = Guid.Parse("84f49492-cda4-42b7-a3ed-28a1ba57e9bc"), Name = "Автономне опалення" }
            );
        }
    }
}