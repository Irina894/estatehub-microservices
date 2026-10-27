using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

/// <summary>Єдина точка доступу до MongoDB (реєструється як singleton: MongoClient потокобезпечний).</summary>
public sealed class MongoDbContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<Review> Reviews { get; }

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var o = options.Value;
        var client = new MongoClient(o.ConnectionString);
        Database = client.GetDatabase(o.DatabaseName);
        Reviews = Database.GetCollection<Review>(o.ReviewsCollection);
    }
}
