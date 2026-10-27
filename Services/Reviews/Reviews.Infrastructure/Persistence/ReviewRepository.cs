using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Infrastructure.Persistence;

public sealed class ReviewRepository(MongoDbContext context) : IReviewRepository
{
    private IMongoCollection<Review> Col => context.Reviews;

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct) =>
        await Col.Find(r => r.Id == id).FirstOrDefaultAsync(ct);

    public async Task<bool> ExistsAsync(long productId, long authorUserId, CancellationToken ct) =>
        await Col.Find(r => r.ProductId == productId && r.Author.UserId == authorUserId)
                 .Limit(1)
                 .AnyAsync(ct);

    public Task AddAsync(Review review, CancellationToken ct) =>
        Col.InsertOneAsync(review, cancellationToken: ct);

    public async Task UpdateAsync(Review review, CancellationToken ct)
    {
        var expectedVersion = review.Version;
        review.BumpVersion();

        // Замінюємо документ лише якщо в БД усе ще та версія, яку ми прочитали
        var result = await Col.ReplaceOneAsync(
            r => r.Id == review.Id && r.Version == expectedVersion,
            review,
            cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new ConflictException(
                $"Конфлікт версій: відгук {review.Id} було змінено або видалено іншим запитом (очікувалась версія {expectedVersion}).");
    }

    public async Task<bool> DeleteAsync(ObjectId id, CancellationToken ct)
    {
        var result = await Col.DeleteOneAsync(r => r.Id == id, ct);
        return result.DeletedCount > 0;
    }
}
