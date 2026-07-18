namespace SharedKernel.Results;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5,
    RateLimitExceeded = 6,
    BusinessRule = 7,
    Infrastructure = 8,
    ExternalService = 9,
    Unexpected = 10
}