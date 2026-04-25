namespace NakhlaBelal.Api.Wrappers;

public class PagedResponse<T>
{
    public bool Success { get; init; } = true;
    public IEnumerable<T> Data { get; init; } = Enumerable.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
