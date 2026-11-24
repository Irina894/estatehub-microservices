namespace Reviews.Application.Common.Models;

public sealed class AuthorDto
{
    public long UserId { get; set; }
    public string DisplayName { get; set; } = default!;
}

public sealed class CriteriaDto
{
    public int Location { get; set; }
    public int Condition { get; set; }
    public int Price { get; set; }
}

public sealed class CommentDto
{
    public long AuthorId { get; set; }
    public string Text { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Повна модель відгуку для відповіді API.</summary>
public sealed class ReviewDto
{
    public string Id { get; set; } = default!;
    public long ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public AuthorDto Author { get; set; } = default!;
    public int Rating { get; set; }
    public string Text { get; set; } = default!;
    public CriteriaDto? Criteria { get; set; }
    public List<CommentDto> Comments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Version { get; set; }
}

/// <summary>Коротка модель для списків (проєкція: без коментарів).</summary>
public sealed class ReviewListItemDto
{
    public string Id { get; set; } = default!;
    public long ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string AuthorName { get; set; } = default!;
    public int Rating { get; set; }
    public string Text { get; set; } = default!;
    public int CommentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
