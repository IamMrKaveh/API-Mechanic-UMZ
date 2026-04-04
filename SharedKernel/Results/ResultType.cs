namespace SharedKernel.Results;

public enum ResultType
{
    Ok,
    BadRequest,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Unexpected,
    RateLimitExceeded
}