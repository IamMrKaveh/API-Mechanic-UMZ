namespace Application.Common.Models;

public class ServiceResult
{
    public ServiceResultStatus Status { get; protected init; }
    public int StatusCode { get; protected set; } = 200;
    public bool IsSuccess => (int)Status >= 200 && (int)Status < 300;
    public bool IsFailed { get; protected set; }
    public string? Error { get; protected set; }

    public static ServiceResult Success() => new()
    {
        IsSuccess = true,
        IsFailed = false,
    };

    public static ServiceResult Failure(string error, int statusCode = 400) =>
        new()
        {
            IsFailed = true,
            IsSuccess = false,
            Error = error,
            StatusCode = statusCode
        };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private init; }

    public static ServiceResult<T> Success(T data) => new()
    {
        IsSuccess = true,
        IsFailed = false,
        Value = data,
        StatusCode = 200
    };

    public new static ServiceResult<T> Failure(string error, int statusCode = 400) => new()
    {
        IsFailed = true,
        IsSuccess = false,
        Error = error,
        StatusCode = statusCode
    };

    public static ServiceResult<T> Ok(T value) => new()
    {
        Value = value,
        Status = ServiceResultStatus.Success
    };

    public static ServiceResult<T> Created(T value) => new()
    {
        Value = value,
        Status = ServiceResultStatus.Created
    };

    public static ServiceResult<T> NotFound(string? error = null) => new()
    {
        Status = ServiceResultStatus.NotFound,
        Error = error
    };

    public static ServiceResult<T> BadRequest(string? error = null) => new()
    {
        Status = ServiceResultStatus.BadRequest,
        Error = error
    };

    public static ServiceResult<T> Unauthorized(string? error = null) => new()
    {
        Status = ServiceResultStatus.Unauthorized,
        Error = error
    };

    public static ServiceResult<T> Forbidden(string? error = null) => new()
    {
        Status = ServiceResultStatus.Forbidden,
        Error = error
    };

    public static ServiceResult<T> Conflict(string? error = null) => new()
    {
        Status = ServiceResultStatus.Conflict,
        Error = error
    };

    public static ServiceResult<T> Unprocessable(string? error = null) => new()
    {
        Status = ServiceResultStatus.UnprocessableEntity,
        Error = error
    };

    public static ServiceResult<T> Fail(string? error = null) => new()
    {
        Status = ServiceResultStatus.InternalError,
        Error = error
    };
}