namespace Application.Common.Validation;

/// <summary>
/// The pagination limits every paged query is held to.
/// </summary>
public static class PaginationDefaults
{
    /// <summary>The largest number of rows a single page may return.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The page size used when a caller does not provide one.</summary>
    public const int DefaultPageSize = 20;
}
