namespace SharedKernel.Results;

public sealed record ValidationError(
    string Property,
    string Message,
    string? Code = null,
    object? AttemptedValue = null);