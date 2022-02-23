namespace VoidCat.Model;

public abstract class PagedResult
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Pages => TotalResults / PageSize;
    public int TotalResults { get; init; }
}

public sealed class PagedResult<T> : PagedResult
{
    public IAsyncEnumerable<T> Results { get; init; }

    public async Task<RenderedResults<T>> GetResults()
    {
        return new()
        {
            Page = Page,
            PageSize = PageSize,
            TotalResults = TotalResults,
            Results = await Results.ToListAsync()
        };
    }
}

public sealed class RenderedResults<T> : PagedResult
{
    public IList<T> Results { get; init; }
}

public sealed record PagedRequest(int Page, int PageSize, PagedSortBy SortBy = PagedSortBy.Name, PageSortOrder SortOrder = PageSortOrder.Asc);

public enum PagedSortBy : byte
{
    Name,
    Date,
    Size,
    Id
}

public enum PageSortOrder : byte
{
    Asc,
    Dsc
}