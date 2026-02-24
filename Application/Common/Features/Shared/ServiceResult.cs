namespace Application.Common.Features.Shared;

public class ServiceResult
{
    public bool IsSucceed { get; protected set; }
    public bool IsFailed { get; protected set; }
    public string? Error { get; protected set; }
    public int StatusCode { get; protected set; } = 200;

    public static ServiceResult Success() => new()
    {
        IsSucceed = true,
        IsFailed = false,
    };

    public static ServiceResult Failure(string error, int statusCode = 400) =>
        new()
        {
            IsFailed = true,
            IsSucceed = false,
            Error = error,
            StatusCode = statusCode
        };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private set; }

    public static ServiceResult<T> Success(T data) => new()
    {
        IsSucceed = true,
        IsFailed = false,
        Data = data,
        StatusCode = 200
    };

    public new static ServiceResult<T> Failure(string error, int statusCode = 400) => new()
    {
        IsFailed = true,
        IsSucceed = false,
        Error = error,
        StatusCode = statusCode
    };
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => Convert.ToInt32(Math.Ceiling((double)TotalCount / PageSize));
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PaginatedResult<T> Create(List<T> items, int totalCount, int page, int pageSize)
    {
        return new PaginatedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}