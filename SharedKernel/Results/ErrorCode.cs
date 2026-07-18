namespace SharedKernel.Results;

public static class ErrorCode
{
    public const string None = "";
    public const string Unexpected = "GEN_UNEXPECTED";
    public const string Validation = "GEN_VALIDATION";
    public const string NotFound = "GEN_NOT_FOUND";
    public const string Conflict = "GEN_CONFLICT";
    public const string Unauthorized = "GEN_UNAUTHORIZED";
    public const string Forbidden = "GEN_FORBIDDEN";
    public const string RateLimitExceeded = "GEN_RATE_LIMIT";
    public const string BusinessRule = "GEN_BUSINESS_RULE";
    public const string Infrastructure = "GEN_INFRASTRUCTURE";
    public const string ExternalService = "GEN_EXTERNAL_SERVICE";
    public const string Failure = "GEN_FAILURE";
}