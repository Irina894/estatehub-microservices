using MongoDB.Bson;
using Reviews.Application.Commands.AddComment;
using Reviews.Application.Commands.DeleteReview;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;
using Xunit;

namespace Reviews.Tests;

public class LimitsAndHandlersTests
{
    private static Review NewReview() => new(1, "Квартира", new Author(10, "Іра"), 5, "Гарно");

    // ---------- доменні ліміти ----------

    [Fact]
    public void AddComment_OverLimit_ThrowsDomainException()
    {
        var review = NewReview();
        for (var i = 0; i < Review.MaxComments; i++)
            review.AddComment(5, $"Коментар {i}");

        Assert.Throws<DomainException>(() => review.AddComment(5, "Зайвий"));
        Assert.Equal(Review.MaxComments, review.Comments.Count);
    }

    [Fact]
    public void Comment_WithTooLongText_ThrowsDomainException() =>
        Assert.Throws<DomainException>(() => new Comment(5, new string('x', Comment.TextMaxLength + 1)));

    [Fact]
    public void Review_WithTooLongText_ThrowsDomainException() =>
        Assert.Throws<DomainException>(() =>
            new Review(1, "Квартира", new Author(10, "Іра"), 5, new string('x', Review.TextMaxLength + 1)));

    // ---------- обробники з підробленим репозиторієм ----------

    private sealed class FakeRepo : IReviewRepository
    {
        public List<Review> Store { get; } = new();

        public Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct) =>
            Task.FromResult(Store.FirstOrDefault(r => r.Id == id));

        public Task<bool> ExistsAsync(long p, long a, CancellationToken ct) =>
            Task.FromResult(Store.Any(r => r.ProductId == p && r.Author.UserId == a));

        public Task AddAsync(Review r, CancellationToken ct) { Store.Add(r); return Task.CompletedTask; }
        public Task UpdateAsync(Review r, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> DeleteAsync(ObjectId id, CancellationToken ct) =>
            Task.FromResult(Store.RemoveAll(r => r.Id == id) > 0);
    }

    [Fact]
    public async Task AddComment_ForMissingReview_ThrowsNotFound()
    {
        // mapper не потрібен: виняток виникає раніше за мапінг
        var handler = new AddCommentCommandHandler(new FakeRepo(), null!);
        var command = new AddCommentCommand(ObjectId.GenerateNewId().ToString(), 5, "Дякую");

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_ForMissingReview_ThrowsNotFound()
    {
        var handler = new DeleteReviewCommandHandler(new FakeRepo());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteReviewCommand(ObjectId.GenerateNewId().ToString()), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_ForExistingReview_RemovesIt()
    {
        var repo = new FakeRepo();
        var review = NewReview();
        repo.Store.Add(review);

        await new DeleteReviewCommandHandler(repo).Handle(new DeleteReviewCommand(review.Id.ToString()), CancellationToken.None);

        Assert.Empty(repo.Store);
    }
}
