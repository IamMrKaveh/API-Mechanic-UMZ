namespace SharedKernel.Models;

public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize > 0
        ? (TotalCount + PageSize - 1) / PageSize
        : 0;

    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
    public bool IsEmpty => TotalCount == 0;

    public PaginatedResult()
    { }

    public PaginatedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCount));

        if (page <= 0)
            throw new ArgumentOutOfRangeException(nameof(page));

        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        Items = items is List<T> list
            ? list.AsReadOnly()
            : new List<T>(items).AsReadOnly();

        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public static PaginatedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCount));

        if (page <= 0)
            throw new ArgumentOutOfRangeException(nameof(page));

        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        var list = items as List<T> ?? items.ToList();

        return new PaginatedResult<T>(
            list.AsReadOnly(),
            totalCount,
            page,
            pageSize);
    }

    public void Deconstruct(
        out IReadOnlyList<T> items,
        out int totalCount,
        out int page,
        out int pageSize)
    {
        items = Items;
        totalCount = TotalCount;
        page = Page;
        pageSize = PageSize;
    }
}