namespace Catalog.Application.Products;
public class PagedList<T>
{
    private PagedList(IEnumerable<T> items, int page, int pageSize, long totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
    private PagedList()
    {
    }

    public IEnumerable<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public long TotalCount { get; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page * PageSize < TotalCount;

    public static PagedList<T> Create(IEnumerable<T> items, int page, int pageSize, long totalCount)
    {
        return new(items, page, pageSize, totalCount);
    }

}