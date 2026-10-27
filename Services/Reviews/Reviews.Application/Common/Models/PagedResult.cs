namespace Reviews.Application.Common.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
