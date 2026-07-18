namespace SharedKernel.Results;

public sealed record Error(
    string Code,
    string Message,
    ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
    public IReadOnlyList<ValidationError>? ValidationErrors { get; init; }
    public IReadOnlyList<Error>? InnerErrors { get; init; }

    public Error WithMetadata(IReadOnlyDictionary<string, object?> metadata)
        => this with { Metadata = metadata };

    public Error WithValidationErrors(IReadOnlyList<ValidationError> errors)
        => this with { ValidationErrors = errors };

    public Error WithInnerErrors(IReadOnlyList<Error> errors)
        => this with { InnerErrors = errors };

    public static Error Failure(string message)
        => new(ErrorCode.Failure, message, ErrorType.Failure);

    public static Error Failure(string code, string message)
        => new(code, message, ErrorType.Failure);

    public static Error Validation(string message)
        => new(ErrorCode.Validation, message, ErrorType.Validation);

    public static Error Validation(string code, string message)
        => new(code, message, ErrorType.Validation);

    public static Error NotFound(string message)
        => new(ErrorCode.NotFound, message, ErrorType.NotFound);

    public static Error NotFound(string code, string message)
        => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string message)
        => new(ErrorCode.Conflict, message, ErrorType.Conflict);

    public static Error Conflict(string code, string message)
        => new(code, message, ErrorType.Conflict);

    public static Error Forbidden(string message)
        => new(ErrorCode.Forbidden, message, ErrorType.Forbidden);

    public static Error Forbidden(string code, string message)
        => new(code, message, ErrorType.Forbidden);

    public static Error Unauthorized(string message)
        => new(ErrorCode.Unauthorized, message, ErrorType.Unauthorized);

    public static Error Unauthorized(string code, string message)
        => new(code, message, ErrorType.Unauthorized);

    public static Error RateLimitExceeded(string message)
        => new(ErrorCode.RateLimitExceeded, message, ErrorType.RateLimitExceeded);

    public static Error BusinessRule(string code, string message)
        => new(code, message, ErrorType.BusinessRule);

    public static Error Infrastructure(string message)
        => new(ErrorCode.Infrastructure, message, ErrorType.Infrastructure);

    public static Error ExternalService(string message)
        => new(ErrorCode.ExternalService, message, ErrorType.ExternalService);

    public static Error Unexpected(string message)
        => new(ErrorCode.Unexpected, message, ErrorType.Unexpected);
}