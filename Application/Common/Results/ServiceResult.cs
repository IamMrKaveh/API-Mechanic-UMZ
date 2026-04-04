using SharedKernel.Results;

namespace Application.Common.Results;

public class ServiceResult
{
    public Result InnerResult { get; }

    public bool IsSuccess => InnerResult.IsSuccess;
    public bool IsFailure => InnerResult.IsFailure;
    public string? Error => InnerResult.Error.Message;
    public int StatusCode => MapToStatusCode(InnerResult.Type);

    protected ServiceResult(Result innerResult)
    {
        InnerResult = innerResult;
    }

    public static ServiceResult Success() => new(Result.Success());

    public static ServiceResult Failure(string error, int statusCode = 400) => new(Result.Failure(new Error("Failure", error), MapFromStatusCode(statusCode)));

    public static ServiceResult NotFound(string error) => new(Result.Failure(new Error("NotFound", error), ResultType.NotFound));

    public static ServiceResult Conflict(string error) => new(Result.Failure(new Error("Conflict", error), ResultType.Conflict));

    public static ServiceResult Unauthorized(string error) => new(Result.Failure(new Error("Unauthorized", error), ResultType.Unauthorized));

    public static ServiceResult Forbidden(string error) => new(Result.Failure(new Error("Forbidden", error), ResultType.Forbidden));

    public static ServiceResult Unexpected(string error) => new(Result.Failure(new Error("Unexpected", error), ResultType.Unexpected));

    public static ServiceResult RateLimitExceeded(string error) => new(Result.Failure(new Error("RateLimitExceeded", error), ResultType.RateLimitExceeded));

    public static ServiceResult RateLimitReached(string error) => new(Result.Failure(new Error("RateLimitReached", error), ResultType.RateLimitExceeded));

    public static ServiceResult Validation(string error) => new(Result.Failure(new Error("Validation", error), ResultType.BadRequest));

    protected static int MapToStatusCode(ResultType type) => type switch
    {
        ResultType.Ok => 200,
        ResultType.BadRequest => 400,
        ResultType.Unauthorized => 401,
        ResultType.Forbidden => 403,
        ResultType.NotFound => 404,
        ResultType.Conflict => 409,
        ResultType.RateLimitExceeded => 429,
        ResultType.Unexpected => 500,
        _ => 400
    };

    protected static ResultType MapFromStatusCode(int statusCode) => statusCode switch
    {
        200 => ResultType.Ok,
        400 => ResultType.BadRequest,
        401 => ResultType.Unauthorized,
        403 => ResultType.Forbidden,
        404 => ResultType.NotFound,
        409 => ResultType.Conflict,
        429 => ResultType.RateLimitExceeded,
        500 => ResultType.Unexpected,
        _ => ResultType.BadRequest
    };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value => ((Result<T>)InnerResult).Value;

    private ServiceResult(Result<T> innerResult) : base(innerResult)
    {
    }

    public static ServiceResult<T> Success(T value) => new(Result<T>.Success(value));

    public new static ServiceResult<T> Failure(string error, int statusCode = 400) => new(Result<T>.Failure(new Error("Failure", error), MapFromStatusCode(statusCode)));

    public new static ServiceResult<T> NotFound(string error) => new(Result<T>.Failure(new Error("NotFound", error), ResultType.NotFound));

    public new static ServiceResult<T> Conflict(string error) => new(Result<T>.Failure(new Error("Conflict", error), ResultType.Conflict));

    public new static ServiceResult<T> Unauthorized(string error) => new(Result<T>.Failure(new Error("Unauthorized", error), ResultType.Unauthorized));

    public new static ServiceResult<T> Forbidden(string error) => new(Result<T>.Failure(new Error("Forbidden", error), ResultType.Forbidden));

    public new static ServiceResult<T> Unexpected(string error) => new(Result<T>.Failure(new Error("Unexpected", error), ResultType.Unexpected));

    public new static ServiceResult<T> RateLimitExceeded(string error) => new(Result<T>.Failure(new Error("RateLimitExceeded", error), ResultType.RateLimitExceeded));

    public new static ServiceResult<T> RateLimitReached(string error) => new(Result<T>.Failure(new Error("RateLimitReached", error), ResultType.RateLimitExceeded));

    public new static ServiceResult<T> Validation(string error) => new(Result<T>.Failure(new Error("Validation", error), ResultType.BadRequest));
}