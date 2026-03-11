namespace Domain.Common.Results;

public sealed class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);

    public Result<T> Map<T>(Func<T> mapper)
        => IsSuccess ? Result<T>.Success(mapper()) : Result<T>.Failure(Error!);

    public static implicit operator bool(Result result) => result.IsSuccess;
}

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public T? Value { get; }
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(string error) => new(false, default, error);

    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Success(mapper(Value!)) : Result<TOut>.Failure(Error!);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
        => IsSuccess ? binder(Value!) : Result<TOut>.Failure(Error!);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);

    public static implicit operator bool(Result<T> result) => result.IsSuccess;
}