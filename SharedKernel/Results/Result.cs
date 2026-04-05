namespace SharedKernel.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException(string.Empty);

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException(string.Empty);

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static implicit operator bool(Result result) => result.IsSuccess;

    public ResultType ToResultType() => IsSuccess ? ResultType.Ok : Error.Type switch
    {
        ErrorType.Validation => ResultType.BadRequest,
        ErrorType.NotFound => ResultType.NotFound,
        ErrorType.Conflict => ResultType.Conflict,
        ErrorType.Unauthorized => ResultType.Unauthorized,
        ErrorType.Forbidden => ResultType.Forbidden,
        _ => ResultType.Unexpected
    };
}