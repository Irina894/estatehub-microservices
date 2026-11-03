using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;
using Xunit;

namespace Reviews.Tests;

/// <summary>
/// Перевіряє BSON-відображення БЕЗ запущеної MongoDB: як виглядає документ і чи читається він назад.
/// </summary>
public class MongoMappingTests
{
    public MongoMappingTests() => MongoMappings.Register();

    private static Review Full()
    {
        var review = new Review(1, "Квартира", new Author(10, "Іра"), 5, "Гарно", new Criteria(5, 4, 3));
        review.AddComment(5, "Дякую");
        return review;
    }

    [Fact]
    public void Document_has_camelCase_fields_and_no_dotnet_internals()
    {
        var doc = Full().ToBsonDocument();

        Assert.True(doc["_id"].IsObjectId);
        Assert.Equal(10, doc["author"]["userId"].ToInt64());
        Assert.Equal("Іра", doc["author"]["displayName"].AsString);
        Assert.True(doc["comments"].IsBsonArray);          // приватне поле _comments під ім'ям comments
        Assert.Equal(0, doc["version"].ToInt32());

        Assert.False(doc.Contains("_comments"));
        Assert.False(doc.Contains("Comments"));
        Assert.False(doc.Contains("Id"));
        Assert.False(doc.Contains("_t"));                  // без дискримінатора типу
    }

    [Fact]
    public void Round_trip_preserves_value_objects_and_comments()
    {
        var original = Full();

        var restored = BsonSerializer.Deserialize<Review>(original.ToBsonDocument());

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Author, restored.Author);        // Value Object: рівність за значенням
        Assert.Equal(original.Criteria, restored.Criteria);
        Assert.Equal(5, restored.Rating);
        Assert.Single(restored.Comments);
        Assert.Equal("Дякую", restored.Comments[0].Text);
    }

    [Fact]
    public void Legacy_document_with_extra_and_missing_fields_is_readable()
    {
        // документ у стилі Лаб. 01: немає version і comments, є зайве поле (гнучка схема)
        var legacy = BsonDocument.Parse("""
        {
          "_id": { "$oid": "66f0c2a1b2c3d4e5f6000001" },
          "productId": 1,
          "productName": "Квартира",
          "author": { "userId": 10, "displayName": "Іра" },
          "rating": 4,
          "text": "Ок",
          "someOldField": "ignored"
        }
        """);

        var review = BsonSerializer.Deserialize<Review>(legacy);

        Assert.Equal(4, review.Rating);
        Assert.Equal(0, review.Version);
        Assert.Empty(review.Comments);
    }
}
