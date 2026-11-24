using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
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
        // трасування команд MongoDB: кожна команда стає span в OpenTelemetry (Лаб. 5)
        var settings = MongoClientSettings.FromConnectionString(o.ConnectionString);
        settings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
        var client = new MongoClient(settings);
        Database = client.GetDatabase(o.DatabaseName);
        Reviews = Database.GetCollection<Review>(o.ReviewsCollection);
    }
}
