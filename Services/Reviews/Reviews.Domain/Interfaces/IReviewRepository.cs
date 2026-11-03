using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Domain.Interfaces;

/// <summary>Репозиторій для запису (команди). Читання для списків робить IReviewQueries.</summary>
public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct);
    Task<bool> ExistsAsync(long productId, long authorUserId, CancellationToken ct);
    Task AddAsync(Review review, CancellationToken ct);

    /// <summary>Оптимістична конкурентність: заміна лише якщо версія в БД збігається. Інакше ConflictException.</summary>
    Task UpdateAsync(Review review, CancellationToken ct);
    Task<bool> DeleteAsync(ObjectId id, CancellationToken ct);
}
