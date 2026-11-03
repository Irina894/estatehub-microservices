using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Seeding;

/// <summary>
/// Програмний ідемпотентний сідінг (викликається після MigrateAsync).
/// Довідники (Categories/Amenities) заливає HasData у міграції; тут — демо-об'єкти.
/// Повторний запуск нічого не дублює.
/// </summary>
public static class DbInitializer
{
    private static readonly Guid Owner1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Owner2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(CatalogDbContext db, CancellationToken ct = default)
    {
        // IgnoreQueryFilters — щоб м'яко видалені записи теж рахувалися «існуючими»
        if (await db.Properties.IgnoreQueryFilters().AnyAsync(ct))
            return;

        var data = new (Guid Cat, Guid Owner, string Title, string Desc, decimal Price, string City, string Addr, double Lat, double Lon, Guid[] Amen)[]
        {
            (CatalogSeedIds.CategoryApartment, Owner1, "2-кімнатна квартира в центрі", "Світла квартира після ремонту, поруч парк.", 68000m, "Чернівці", "вул. Головна, 12", 48.2915, 25.9403, new[] { CatalogSeedIds.AmenityHeating, CatalogSeedIds.AmenityBalcony }),
            (CatalogSeedIds.CategoryApartment, Owner1, "1-кімнатна біля університету", "Для студентів чи молодої пари.", 42000m, "Чернівці", "вул. Коцюбинського, 2", 48.3, 25.93, new[] { CatalogSeedIds.AmenityHeating }),
            (CatalogSeedIds.CategoryHouse, Owner2, "Будинок з садом", "Дворівневий будинок із ділянкою 8 соток.", 135000m, "Чернівці", "вул. Садова, 5", 48.27, 25.95, new[] { CatalogSeedIds.AmenityHeating, CatalogSeedIds.AmenityParking }),
            (CatalogSeedIds.CategoryApartment, Owner2, "3-кімнатна на Оболоні", "Панорамні вікна, новобудова.", 112000m, "Київ", "просп. Героїв Сталінграда, 10", 50.5, 30.5, new[] { CatalogSeedIds.AmenityElevator, CatalogSeedIds.AmenityParking, CatalogSeedIds.AmenityAirConditioning }),
            (CatalogSeedIds.CategoryOffice, Owner1, "Офіс у бізнес-центрі", "Open space на 12 робочих місць.", 95000m, "Київ", "вул. Хрещатик, 22", 50.45, 30.52, new[] { CatalogSeedIds.AmenityElevator, CatalogSeedIds.AmenityAirConditioning }),
            (CatalogSeedIds.CategoryApartment, Owner1, "Студія на Подолі", "Компактна студія з сучасним ремонтом.", 54000m, "Київ", "вул. Сагайдачного, 7", 50.46, 30.51, new[] { CatalogSeedIds.AmenityHeating }),
            (CatalogSeedIds.CategoryHouse, Owner2, "Котедж біля лісу", "Тихий район, камін, гараж.", 210000m, "Львів", "вул. Лісова, 3", 49.84, 24.03, new[] { CatalogSeedIds.AmenityHeating, CatalogSeedIds.AmenityParking }),
            (CatalogSeedIds.CategoryApartment, Owner2, "Квартира у старому місті", "Австрійський будинок, високі стелі.", 98000m, "Львів", "вул. Руська, 15", 49.841, 24.031, new[] { CatalogSeedIds.AmenityBalcony }),
            (CatalogSeedIds.CategoryOffice, Owner1, "Офіс біля Ринку", "Приміщення 80 м², окремий вхід.", 70000m, "Львів", "пл. Ринок, 9", 49.8419, 24.0315, new[] { CatalogSeedIds.AmenityAirConditioning }),
            (CatalogSeedIds.CategoryApartment, Owner2, "Квартира з видом на море", "Аркадія, 10 хвилин до пляжу.", 150000m, "Одеса", "вул. Генуезька, 1", 46.43, 30.76, new[] { CatalogSeedIds.AmenityElevator, CatalogSeedIds.AmenityBalcony }),
            (CatalogSeedIds.CategoryHouse, Owner1, "Будинок у Лузанівці", "Новий будинок, 4 спальні.", 175000m, "Одеса", "вул. Піонерська, 20", 46.5, 30.7, new[] { CatalogSeedIds.AmenityParking }),
            (CatalogSeedIds.CategoryApartment, Owner1, "2-кімнатна на Салтівці", "Поруч метро, є паркінг у дворі.", 39000m, "Харків", "просп. Героїв Харкова, 40", 50.0, 36.3, new[] { CatalogSeedIds.AmenityParking, CatalogSeedIds.AmenityHeating }),
        };

        var n = 0;
        foreach (var d in data)
        {
            var p = new Property(d.Cat, d.Owner, d.Title, d.Desc, d.Price, d.City);
            p.SetLocation(d.Addr, d.Lat, d.Lon);
            n++;
            p.AddImage($"https://picsum.photos/seed/estate{n}a/800/600");
            p.AddImage($"https://picsum.photos/seed/estate{n}b/800/600");
            foreach (var a in d.Amen) p.AddAmenity(a);
            db.Properties.Add(p);
        }

        await db.SaveChangesAsync(ct);
    }
}
