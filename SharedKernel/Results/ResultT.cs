namespace SharedKernel.Results;

public sealed class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value when result is failure.");

    private Result(T value) : base(true, Error.None)
    {
        _value = value;
    }

    private Result(Error error) : base(false, error)
    {
        _value = default;
    }

    public static Result<T> Success(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return new Result<T>(value);
    }

    public new static Result<T> Failure(Error error)
        => new(error);

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess
            ? Result<TOut>.Success(mapper(Value))
            : Result<TOut>.Failure(Error);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
        => IsSuccess
            ? binder(Value)
            : Result<TOut>.Failure(Error);

    public TOut Match<TOut>(
        Func<T, TOut> onSuccess,
        Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(Error);

    public static implicit operator bool(Result<T> result) => result.IsSuccess;
}