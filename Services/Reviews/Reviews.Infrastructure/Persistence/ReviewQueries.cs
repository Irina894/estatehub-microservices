using AutoMapper;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Domain.Entities;

namespace Reviews.Infrastructure.Persistence;

/// <summary>Сторона читання: фільтрація, сортування, skip/limit і проєкція виконуються на боці MongoDB.</summary>
public sealed class ReviewQueries(MongoDbContext context, IMapper mapper) : IReviewQueries
{
    public async Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        if (!ObjectId.TryParse(id, out var oid)) return null;
        var review = await context.Reviews.Find(r => r.Id == oid).FirstOrDefaultAsync(ct);
        return review is null ? null : mapper.Map<ReviewDto>(review);
    }

    public async Task<PagedResult<ReviewListItemDto>> GetListAsync(
        long? productId, int? minRating, string? search, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var f = Builders<Review>.Filter;
        var filters = new List<FilterDefinition<Review>>();
        if (productId.HasValue) filters.Add(f.Eq(r => r.ProductId, productId.Value));
        if (minRating.HasValue) filters.Add(f.Gte(r => r.Rating, minRating.Value));
        if (!string.IsNullOrWhiteSpace(search)) filters.Add(f.Text(search));
        var filter = filters.Count == 0 ? f.Empty : f.And(filters);

        var total = await context.Reviews.CountDocumentsAsync(filter, cancellationToken: ct);

        // проєкція в BsonDocument: коментарі не вантажимо, лише їхню кількість ($size)
        var projection = new BsonDocument
        {
            { "productId", 1 },
            { "productName", 1 },
            { "authorName", "$author.displayName" },
            { "rating", 1 },
            { "text", 1 },
            { "createdAt", 1 },
            { "commentsCount", new BsonDocument("$size", new BsonDocument("$ifNull", new BsonArray { "$comments", new BsonArray() })) }
        };

        var docs = await context.Reviews.Aggregate()
            .Match(filter)
            .Sort(Builders<Review>.Sort.Descending(r => r.CreatedAt).Descending(r => r.Id))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<BsonDocument>(projection)
            .ToListAsync(ct);

        var items = docs.Select(d => new ReviewListItemDto
        {
            Id = d["_id"].AsObjectId.ToString(),
            ProductId = d["productId"].ToInt64(),
            ProductName = d["productName"].AsString,
            AuthorName = d["authorName"].AsString,
            Rating = d["rating"].ToInt32(),
            Text = d["text"].AsString,
            CommentsCount = d["commentsCount"].ToInt32(),
            CreatedAt = d["createdAt"].ToUniversalTime()
        }).ToList();

        return new PagedResult<ReviewListItemDto>(items, total, page, pageSize);
    }
}
