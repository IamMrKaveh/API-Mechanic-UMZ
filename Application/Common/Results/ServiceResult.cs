using SharedKernel.Results;

namespace Application.Common.Results;

public class ServiceResult
{
    public bool IsSuccess { get; protected init; }
    public bool IsFailed => !IsSuccess;
    public string? Error { get; protected init; }
    public ErrorType Type { get; protected init; }

    protected ServiceResult()
    { }

    public static ServiceResult Success() =>
        new() { IsSuccess = true };

    public static ServiceResult Failure(string error, ErrorType type = ErrorType.Failure) =>
        new() { IsSuccess = false, Error = error, Type = type };

    public static ServiceResult Validation(string error) =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.Validation };

    public static ServiceResult NotFound(string error = "یافت نشد.") =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.NotFound };

    public static ServiceResult Conflict(string error) =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.Conflict };

    public static ServiceResult Unauthorized(string error = "دسترسی غیرمجاز.") =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.Unauthorized };

    public static ServiceResult Forbidden(string error = "دسترسی ممنوع.") =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.Forbidden };

    public static ServiceResult RateLimitExceeded(string error = "درخواست بیش از حد") =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.RateLimitExceeded };

    public static ServiceResult FromResult(Result result) =>
        result.IsSuccess ? Success() : Failure(result.Error.Message, result.Error.Type);
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; private init; }

    public static ServiceResult<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    public new static ServiceResult<T> Failure(string error, ErrorType type = ErrorType.Failure) =>
        new() { IsSuccess = false, Error = error, Type = type };

    public new static ServiceResult<T> Validation(string error) =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.Validation };

    public new static ServiceResult<T> NotFound(string error = "یافت نشد.") =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.NotFound };

    public new static ServiceResult<T> Conflict(string error) =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.Conflict };

    public new static ServiceResult<T> Unauthorized(string error = "دسترسی غیرمجاز.") =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.Unauthorized };

    public new static ServiceResult<T> Forbidden(string error = "دسترسی ممنوع.") =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.Forbidden };

    public new static ServiceResult<T> RateLimitExceeded(string error = "درخواست بیش از حد") =>
        new() { IsSuccess = false, Error = error, Type = ErrorType.RateLimitExceeded };

    public static ServiceResult<T> FromResult(Result<T> result) =>
        result.IsSuccess
            ? Success(result.Value)
            : new ServiceResult<T> { IsSuccess = false, Error = result.Error.Message, Type = result.Error.Type };
}