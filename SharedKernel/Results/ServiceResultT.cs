namespace SharedKernel.Results;

public sealed class ServiceResult<T> : ServiceResult
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value when result is failure.");

    public T? ValueOrDefault => _value;

    private ServiceResult(T? value) : base(true, Error.None)
    {
        _value = value;
    }

    private ServiceResult(Error error) : base(false, error)
    {
        _value = default;
    }

    public static ServiceResult<T> Success(T value) => new(value);

    public new static ServiceResult<T> Failure(Error error) => new(error);

    public new static ServiceResult<T> Failure(string message, ErrorType type = ErrorType.Failure)
        => new(new Error(CodeFor(type), message, type));

    public new static ServiceResult<T> Validation(string message)
        => new(Error.Validation(message));

    public new static ServiceResult<T> Validation(IReadOnlyList<ValidationError> errors, string? message = null)
        => new(Error.Validation(message ?? "اطلاعات ورودی نامعتبر است.").WithValidationErrors(errors));

    public new static ServiceResult<T> NotFound(string message = "یافت نشد.")
        => new(Error.NotFound(message));

    public new static ServiceResult<T> Conflict(string message)
        => new(Error.Conflict(message));

    public new static ServiceResult<T> Unauthorized(string message = "دسترسی غیرمجاز.")
        => new(Error.Unauthorized(message));

    public new static ServiceResult<T> Forbidden(string message = "دسترسی ممنوع.")
        => new(Error.Forbidden(message));

    public new static ServiceResult<T> RateLimitExceeded(string message = "درخواست بیش از حد.")
        => new(Error.RateLimitExceeded(message));

    public new static ServiceResult<T> BusinessRule(string code, string message)
        => new(Error.BusinessRule(code, message));

    public new static ServiceResult<T> Unexpected(string message = "خطای غیرمنتظره‌ای رخ داده است.")
        => new(Error.Unexpected(message));

    public ServiceResult<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess
            ? ServiceResult<TOut>.Success(mapper(Value))
            : ServiceResult<TOut>.Failure(Error);

    public ServiceResult<TOut> Bind<TOut>(Func<T, ServiceResult<TOut>> binder)
        => IsSuccess
            ? binder(Value)
            : ServiceResult<TOut>.Failure(Error);

    public ServiceResult<T> Tap(Action<T> action)
    {
        if (IsSuccess) action(Value);
        return this;
    }

    public ServiceResult<T> Ensure(Func<T, bool> predicate, Error error)
    {
        if (IsFailure) return this;
        return predicate(Value) ? this : Failure(error);
    }

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(Error);

    public static implicit operator ServiceResult<T>(T value) => Success(value);

    public static implicit operator ServiceResult<T>(Error error) => Failure(error);

    public static implicit operator bool(ServiceResult<T> result) => result.IsSuccess;
}