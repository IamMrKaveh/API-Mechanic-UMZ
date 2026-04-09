namespace Presentation.Base.Responses;

public record ApiResponse<T>(
    T? Data,
    bool IsSuccess,
    string? Message,
    IDictionary<string, string[]>? Errors = null
);

public record ApiResponse(
    bool IsSuccess,
    string? Message,
    IDictionary<string, string[]>? Errors = null
);

public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int TotalPages,
    int TotalCount,
    bool HasPreviousPage,
    bool HasNextPage
);