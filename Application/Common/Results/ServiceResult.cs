using SharedKernel.Results;

namespace Application.Common.Results;

public class ServiceResult
{
    public bool IsSuccess { get; protected init; }
    public bool IsFailed => !IsSuccess;
    public string? Error { get; protected init; }
    public int StatusCode { get; protected init; }

    protected ServiceResult()
    { }

    public static ServiceResult Success() => new() { IsSuccess = true, StatusCode = 200 };

    public static ServiceResult Failure(string error, int statusCode = 400) => new() { IsSuccess = false, Error = error, StatusCode = statusCode };

    public static ServiceResult NotFound(string error = "یافت نشد.") => new() { IsSuccess = false, Error = error, StatusCode = 404 };

    public static ServiceResult Conflict(string error) => new() { IsSuccess = false, Error = error, StatusCode = 409 };

    public static ServiceResult Unauthorized(string error = "دسترسی غیرمجاز.") => new() { IsSuccess = false, Error = error, StatusCode = 401 };

    public static ServiceResult Forbidden(string error = "دسترسی ممنوع.") => new() { IsSuccess = false, Error = error, StatusCode = 403 };

    public static ServiceResult Unexpected(string error = "خطای داخلی سرور.") => new() { IsSuccess = false, Error = error, StatusCode = 500 };

    public static ServiceResult FromResult(Result result) =>
        result.IsSuccess ? Success() : Failure(result.Error.Message, MapErrorType(result.Error.Type));

    private static int MapErrorType(ErrorType type) => type switch
    {
        ErrorType.NotFound => 404,
        ErrorType.Conflict => 409,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden => 403,
        ErrorType.Validation => 400,
        _ => 500
    };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private init; }

    public static ServiceResult<T> Success(T value) => new() { IsSuccess = true, Value = value, StatusCode = 200 };

    public new static ServiceResult<T> Failure(string error, int statusCode = 400) => new() { IsSuccess = false, Error = error, StatusCode = statusCode };

    public new static ServiceResult<T> NotFound(string error = "یافت نشد.") => new() { IsSuccess = false, Error = error, StatusCode = 404 };

    public new static ServiceResult<T> Conflict(string error) => new() { IsSuccess = false, Error = error, StatusCode = 409 };

    public new static ServiceResult<T> Unauthorized(string error = "دسترسی غیرمجاز.") => new() { IsSuccess = false, Error = error, StatusCode = 401 };

    public new static ServiceResult<T> Unexpected(string error = "خطای داخلی سرور.") => new() { IsSuccess = false, Error = error, StatusCode = 500 };

    public static ServiceResult<T> FromResult(Result<T> result) =>
        result.IsSuccess
            ? Success(result.Value)
            : new ServiceResult<T> { IsSuccess = false, Error = result.Error.Message, StatusCode = MapErrorTypeInternal(result.Error.Type) };

    private static int MapErrorTypeInternal(ErrorType type) => type switch
    {
        ErrorType.NotFound => 404,
        ErrorType.Conflict => 409,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden => 403,
        ErrorType.Validation => 400,
        _ => 500
    };
}