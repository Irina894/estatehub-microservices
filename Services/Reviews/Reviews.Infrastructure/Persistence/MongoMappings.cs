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

            // 1. Конвенції. УВАГА: до ручних BsonClassMap (без cm.AutoMap()) вони НЕ застосовуються,
            //    тому імена елементів нижче задано явно через SetElementName.
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
                cm.MapProperty(x => x.UserId).SetElementName("userId");
                cm.MapProperty(x => x.DisplayName).SetElementName("displayName");
                cm.MapConstructor(typeof(Author).GetConstructor(new[] { typeof(long), typeof(string) })!,
                    nameof(Author.UserId), nameof(Author.DisplayName));
            });

            BsonClassMap.RegisterClassMap<Criteria>(cm =>
            {
                cm.MapProperty(x => x.Location).SetElementName("location");
                cm.MapProperty(x => x.Condition).SetElementName("condition");
                cm.MapProperty(x => x.Price).SetElementName("price");
                cm.MapConstructor(typeof(Criteria).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) })!,
                    nameof(Criteria.Location), nameof(Criteria.Condition), nameof(Criteria.Price));
            });

            BsonClassMap.RegisterClassMap<Comment>(cm =>
            {
                cm.MapProperty(x => x.AuthorId).SetElementName("authorId");
                cm.MapProperty(x => x.Text).SetElementName("text");
                cm.MapProperty(x => x.CreatedAt).SetElementName("createdAt");
                cm.MapConstructor(
                    typeof(Comment).GetConstructor(new[] { typeof(long), typeof(string), typeof(DateTime) })!,
                    nameof(Comment.AuthorId), nameof(Comment.Text), nameof(Comment.CreatedAt));
            });

            // 4. Базова сутність: _id, дати, версія (мапляться там, де оголошені)
            BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
            {
                cm.MapIdProperty(x => x.Id);
                cm.MapProperty(x => x.CreatedAt).SetElementName("createdAt");
                cm.MapProperty(x => x.UpdatedAt).SetElementName("updatedAt");
                cm.MapProperty(x => x.Version).SetElementName("version");
            });

            // 5. Агрегат Review (успадковує мапу BaseEntity)
            BsonClassMap.RegisterClassMap<Review>(cm =>
            {
                cm.MapProperty(x => x.ProductId).SetElementName("productId");
                cm.MapProperty(x => x.ProductName).SetElementName("productName");
                cm.MapProperty(x => x.Author).SetElementName("author");
                cm.MapProperty(x => x.Rating).SetElementName("rating");
                cm.MapProperty(x => x.Text).SetElementName("text");
                cm.MapProperty(x => x.Criteria).SetElementName("criteria");
                cm.MapField("_comments").SetElementName("comments");
            });


            _registered = true;
        }
    }
}
