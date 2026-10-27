namespace Reviews.Infrastructure.Persistence;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "ReviewsDb";
    public string ReviewsCollection { get; set; } = "reviews";
}
