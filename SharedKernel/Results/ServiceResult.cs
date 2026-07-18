namespace SharedKernel.Results;

public class ServiceResult
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public bool IsFailed => !IsSuccess;
    public Error Error { get; }

    protected ServiceResult(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Successful result cannot carry an error.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failed result must carry an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static ServiceResult Success() => new(true, Error.None);

    public static ServiceResult Failure(Error error) => new(false, error);

    public static ServiceResult Failure(string message, ErrorType type = ErrorType.Failure)
        => new(false, new Error(CodeFor(type), message, type));

    public static ServiceResult Validation(string message)
        => Failure(Error.Validation(message));

    public static ServiceResult Validation(IReadOnlyList<ValidationError> errors, string? message = null)
        => Failure(Error.Validation(message ?? "اطلاعات ورودی نامعتبر است.").WithValidationErrors(errors));

    public static ServiceResult NotFound(string message = "یافت نشد.")
        => Failure(Error.NotFound(message));

    public static ServiceResult Conflict(string message)
        => Failure(Error.Conflict(message));

    public static ServiceResult Unauthorized(string message = "دسترسی غیرمجاز.")
        => Failure(Error.Unauthorized(message));

    public static ServiceResult Forbidden(string message = "دسترسی ممنوع.")
        => Failure(Error.Forbidden(message));

    public static ServiceResult RateLimitExceeded(string message = "درخواست بیش از حد.")
        => Failure(Error.RateLimitExceeded(message));

    public static ServiceResult BusinessRule(string code, string message)
        => Failure(Error.BusinessRule(code, message));

    public static ServiceResult Unexpected(string message = "خطای غیرمنتظره‌ای رخ داده است.")
        => Failure(Error.Unexpected(message));

    public ServiceResult Tap(Action action)
    {
        if (IsSuccess) action();
        return this;
    }

    public ServiceResult Ensure(Func<bool> predicate, Error error)
    {
        if (IsFailure) return this;
        return predicate() ? this : Failure(error);
    }

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Error);

    public static implicit operator bool(ServiceResult result) => result.IsSuccess;

    public static implicit operator ServiceResult(Error error) => Failure(error);

    public ResultType ToResultType() => IsSuccess ? ResultType.Ok : Error.Type switch
    {
        ErrorType.Validation => ResultType.BadRequest,
        ErrorType.NotFound => ResultType.NotFound,
        ErrorType.Conflict => ResultType.Conflict,
        ErrorType.Unauthorized => ResultType.Unauthorized,
        ErrorType.Forbidden => ResultType.Forbidden,
        ErrorType.RateLimitExceeded => ResultType.RateLimitExceeded,
        _ => ResultType.Unexpected
    };

    internal static string CodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => ErrorCode.Validation,
        ErrorType.NotFound => ErrorCode.NotFound,
        ErrorType.Conflict => ErrorCode.Conflict,
        ErrorType.Unauthorized => ErrorCode.Unauthorized,
        ErrorType.Forbidden => ErrorCode.Forbidden,
        ErrorType.RateLimitExceeded => ErrorCode.RateLimitExceeded,
        ErrorType.BusinessRule => ErrorCode.BusinessRule,
        ErrorType.Infrastructure => ErrorCode.Infrastructure,
        ErrorType.ExternalService => ErrorCode.ExternalService,
        ErrorType.Unexpected => ErrorCode.Unexpected,
        _ => ErrorCode.Failure
    };
}