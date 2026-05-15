namespace Presentation.Base.Responses;

public record ApiResponse<T>(
    T? Data,
    bool Success,
    string? Message,
    IDictionary<string, string[]>? Errors = null
);

public record ApiResponse(
    bool Success,
    string? Message,
    IDictionary<string, string[]>? Errors = null
);

public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage
);