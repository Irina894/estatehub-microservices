using Xunit;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Reviews.Application.Behaviors;
using Reviews.Application.Commands.CreateReview;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;
using MongoDB.Bson;
using Reviews.Domain.Interfaces;

namespace Reviews.Tests;

public class DomainAndValidationTests
{
    private static Review NewReview(int rating = 5) =>
        new(1, "Квартира", new Author(10, "Іра"), rating, "Гарно");

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(10)]
    public void Review_WithInvalidRating_ThrowsDomainException(int rating) =>
        Assert.Throws<DomainException>(() => NewReview(rating));

    [Fact]
    public void Author_ValueObjects_WithSameValues_AreEqual() =>
        Assert.Equal(new Author(1, "Іра"), new Author(1, "Іра"));

    [Fact]
    public void Author_ValueObjects_WithDifferentValues_AreNotEqual() =>
        Assert.NotEqual(new Author(1, "Іра"), new Author(2, "Іра"));

    [Fact]
    public void AddComment_AppendsAndUpdatesTimestamp()
    {
        var r = NewReview();
        var before = r.UpdatedAt;
        r.AddComment(5, "Дякую");
        Assert.Single(r.Comments);
        Assert.True(r.UpdatedAt >= before);
    }

    private sealed class FakeRepo(bool exists) : IReviewRepository
    {
        public Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct) => Task.FromResult<Review?>(null);
        public Task<bool> ExistsAsync(long p, long a, CancellationToken ct) => Task.FromResult(exists);
        public Task AddAsync(Review r, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Review r, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> DeleteAsync(ObjectId id, CancellationToken ct) => Task.FromResult(true);
    }

    [Fact]
    public async Task Validator_RejectsBadRatingAndEmptyText_WithAllErrors()
    {
        var v = new CreateReviewCommandValidator(new FakeRepo(false));
        var result = await v.ValidateAsync(new CreateReviewCommand(1, "X", 1, "Y", 9, "", null));
        Assert.Contains(result.Errors, e => e.PropertyName == "Rating");
        Assert.Contains(result.Errors, e => e.PropertyName == "Text");
    }

    [Fact]
    public async Task Validator_RejectsDuplicateReview()
    {
        var v = new CreateReviewCommandValidator(new FakeRepo(true));
        var result = await v.ValidateAsync(new CreateReviewCommand(1, "X", 1, "Y", 5, "ok", null));
        Assert.False(result.IsValid);
    }

    private sealed record Ping : IRequest<string>;

    [Fact]
    public async Task ValidationBehavior_WithErrors_ThrowsAndDoesNotCallHandler()
    {
        var failing = new InlineValidator<Ping>();
        failing.RuleFor(x => x).Must(_ => false).WithMessage("fail");
        var behavior = new ValidationBehavior<Ping, string>(
            new IValidator<Ping>[] { failing }, NullLogger<ValidationBehavior<Ping, string>>.Instance);

        var handlerCalled = false;
        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new Ping(), _ => { handlerCalled = true; return Task.FromResult("ok"); }, CancellationToken.None));
        Assert.False(handlerCalled);
    }
}
