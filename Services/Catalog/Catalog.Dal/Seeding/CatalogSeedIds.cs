namespace Catalog.Dal.Seeding;

/// <summary>Фіксовані Id довідників (ті самі, що в db/p2/seed.sql) — потрібні для HasData.</summary>
public static class CatalogSeedIds
{
    public static readonly Guid CategoryOffice = Guid.Parse("149c4f0b-402a-4bc6-8800-47b2c75a40b8");
    public static readonly Guid CategoryHouse = Guid.Parse("2700e123-6447-49f3-8b7c-bbddde9531a2");
    public static readonly Guid CategoryApartment = Guid.Parse("d343467f-5b65-4203-b0e6-5c56d7831411");

    public static readonly Guid AmenityHeating = Guid.Parse("84f49492-cda4-42b7-a3ed-28a1ba57e9bc");
    public static readonly Guid AmenityParking = Guid.Parse("c1fa5853-9dcd-4b8c-ab37-18c7ed6bc402");
    public static readonly Guid AmenityElevator = Guid.Parse("5b0c1a62-7e0d-4a54-9c43-0d6c8f1e2a11");
    public static readonly Guid AmenityBalcony = Guid.Parse("a7d3e9f4-1b2c-4d58-8e61-3f4a5b6c7d22");
    public static readonly Guid AmenityAirConditioning = Guid.Parse("e2f1d0c9-8b7a-4695-a4b3-c2d1e0f9a833");

    public static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
