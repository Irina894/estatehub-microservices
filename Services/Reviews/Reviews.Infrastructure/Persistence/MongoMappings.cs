using Reviews.Domain.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Persistence;

/// <summary>
/// Відображення доменних класів на BSON через BsonClassMap, щоб Domain не залежав від атрибутів драйвера.
/// Метод Register() безпечно викликати кілька разів.
/// </summary>
public static class MongoMappings
{
    private static readonly object Lock = new();
    private static bool _registered;

    public static void Register()
    {
        lock (Lock)
        {
            if (_registered) return;

            // 1. Конвенції: camelCase імена полів, зайві поля в документі ігноруються
            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("ReviewsConventions", pack, _ => true);

            // 2. Усі DateTime зберігаємо/читаємо як UTC
            BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Utc));

            // 3. Value Objects: створюються через конструктор (властивості лише для читання)
            BsonClassMap.RegisterClassMap<Author>(cm =>
            {
                cm.MapProperty(x => x.UserId);
                cm.MapProperty(x => x.DisplayName);
                cm.MapConstructor(typeof(Author).GetConstructor(new[] { typeof(long), typeof(string) })!,
                    nameof(Author.UserId), nameof(Author.DisplayName));
            });

            BsonClassMap.RegisterClassMap<Criteria>(cm =>
            {
                cm.MapProperty(x => x.Location);
                cm.MapProperty(x => x.Condition);
                cm.MapProperty(x => x.Price);
                cm.MapConstructor(typeof(Criteria).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) })!,
                    nameof(Criteria.Location), nameof(Criteria.Condition), nameof(Criteria.Price));
            });

            BsonClassMap.RegisterClassMap<Comment>(cm =>
            {
                cm.MapProperty(x => x.AuthorId);
                cm.MapProperty(x => x.Text);
                cm.MapProperty(x => x.CreatedAt);
                cm.MapConstructor(
                    typeof(Comment).GetConstructor(new[] { typeof(long), typeof(string), typeof(DateTime) })!,
                    nameof(Comment.AuthorId), nameof(Comment.Text), nameof(Comment.CreatedAt));
            });

            // 4. Базова сутність: _id, дати, версія (мапляться там, де оголошені)
            BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
            {
                cm.MapIdProperty(x => x.Id);
                cm.MapProperty(x => x.CreatedAt);
                cm.MapProperty(x => x.UpdatedAt);
                cm.MapProperty(x => x.Version);
            });

            // 5. Агрегат Review (успадковує мапу BaseEntity)
            BsonClassMap.RegisterClassMap<Review>(cm =>
            {
                cm.MapProperty(x => x.ProductId);
                cm.MapProperty(x => x.ProductName);
                cm.MapProperty(x => x.Author);
                cm.MapProperty(x => x.Rating);
                cm.MapProperty(x => x.Text);
                cm.MapProperty(x => x.Criteria);
                cm.MapField("_comments").SetElementName("comments");
            });


            _registered = true;
        }
    }
}
