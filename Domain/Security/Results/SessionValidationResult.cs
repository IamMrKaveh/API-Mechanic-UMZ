namespace Domain.Security.Results;

public sealed class SessionValidationResult
{
    public bool IsValid { get; private set; }
    public bool IsNotFound { get; private set; }
    public bool IsRevoked { get; private set; }
    public bool IsExpired { get; private set; }
    public UserSession? Session { get; private set; }
    public SessionRevocationReason? RevocationReason { get; private set; }
    public string? Error { get; private set; }

    private SessionValidationResult()
    { }

    public static SessionValidationResult Valid(UserSession session) =>
        new()
        {
            IsValid = true,
            Session = session
        };

    public static SessionValidationResult NotFound() =>
        new()
        {
            IsValid = false,
            IsNotFound = true,
            Error = "نشست یافت نشد."
        };

    public static SessionValidationResult Revoked(SessionRevocationReason? reason) =>
        new()
        {
            IsValid = false,
            IsRevoked = true,
            RevocationReason = reason,
            Error = "نشست لغو شده است."
        };

    public static SessionValidationResult Expired() =>
        new()
        {
            IsValid = false,
            IsExpired = true,
            Error = "نشست منقضی شده است."
        };
}