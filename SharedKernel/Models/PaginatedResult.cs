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
        ArgumentNullException.ThrowIfNull(items);

        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        Items = items is List<T> list
            ? list.AsReadOnly()
            : new List<T>(items).AsReadOnly();

        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public static PaginatedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        if (page > 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

            var list = items as List<T> ?? [.. items];

            return new PaginatedResult<T>(
                list.AsReadOnly(),
                totalCount,
                page,
                pageSize);
        }

        throw new ArgumentOutOfRangeException(nameof(page));
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