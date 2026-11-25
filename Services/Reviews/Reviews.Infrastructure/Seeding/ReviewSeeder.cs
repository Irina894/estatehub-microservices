using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Seeding;

/// <summary>Початкові дані. Ідемпотентний: якщо колекція не порожня, нічого не робить.</summary>
public sealed class ReviewSeeder(MongoDbContext context, ILogger<ReviewSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct)
    {
        if (await context.Reviews.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
        {
            logger.LogInformation("Seed пропущено: колекція reviews уже містить дані.");
            return;
        }

        var reviews = new List<Review>
        {
            Make(1, "Квартира на Хрещатику", 101, "Олена К.", 5, "Чудова локація, світла та тепла квартира.", new Criteria(5, 5, 4)),
            Make(1, "Квартира на Хрещатику", 102, "Максим Д.", 4, "Все добре, але шумно ввечері.", new Criteria(5, 4, 3)),
            Make(2, "Будинок у Львові", 103, "Ірина П.", 5, "Затишний будинок, привітний власник.", new Criteria(4, 5, 5)),
            Make(2, "Будинок у Львові", 104, "Андрій С.", 3, "Нормально, але потрібен ремонт.", new Criteria(4, 2, 4)),
            Make(3, "Студія біля метро", 105, "Софія Л.", 4, "Зручно добиратися, компактно.", new Criteria(5, 3, 4)),
            Make(3, "Студія біля метро", 106, "Тарас Б.", 2, "Ціна завищена для такого стану.", new Criteria(4, 3, 1)),
            Make(4, "Апартаменти на Подолі", 107, "Наталія Г.", 5, "Найкращий район, рекомендую.", new Criteria(5, 5, 5)),
            Make(4, "Апартаменти на Подолі", 108, "Юрій М.", 4, "Гарний вид з вікна.", null),
        };
        reviews[0].AddComment(102, "Погоджуюсь, локація чудова!");
        reviews[6].AddComment(101, "Дякую за відгук, теж хочу там жити.");

        await context.Reviews.InsertManyAsync(reviews, cancellationToken: ct);
        logger.LogInformation("Seed виконано: додано {Count} відгуків.", reviews.Count);
    }

    private static Review Make(long productId, string productName, long userId, string name, int rating, string text, Criteria? c) =>
        new(productId, productName, new Author(userId, name), rating, text, c);
}
