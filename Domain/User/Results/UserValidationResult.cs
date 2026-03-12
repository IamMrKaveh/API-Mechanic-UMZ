namespace Domain.User.Results;

public sealed class UserValidationResult
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    private UserValidationResult()
    { }

    public static UserValidationResult Valid() =>
        new() { IsValid = true };

    public static UserValidationResult Invalid(string error) =>
        new() { IsValid = false, Error = error, Errors = new[] { error } };

    public static UserValidationResult Invalid(IEnumerable<string> errors)
    {
        var errorList = errors.ToList().AsReadOnly();
        return new UserValidationResult
        {
            IsValid = false,
            Error = errorList.FirstOrDefault(),
            Errors = errorList
        };
    }

    public string GetErrorsSummary() => string.Join(" | ", Errors);
}