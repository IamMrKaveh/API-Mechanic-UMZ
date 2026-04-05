using Domain.Security.Aggregates;
using Domain.User.ValueObjects;
using SharedKernel.Results;

namespace Domain.Security.Results;

public sealed class SessionCreationResult
{
    public bool IsSuccess { get; private set; }
    public bool IsMaxSessionsExceeded { get; private set; }
    public UserSession? Session { get; private set; }
    public string? Error { get; private set; }

    private SessionCreationResult()
    { }

    public static SessionCreationResult Success(UserSession session) =>
        new()
        {
            IsSuccess = true,
            Session = session
        };

    public static SessionCreationResult MaxSessionsExceeded(UserId userId, int maxSessions) =>
        new()
        {
            IsSuccess = false,
            IsMaxSessionsExceeded = true,
            Error = $"کاربر '{userId}' به حداکثر تعداد نشست‌های فعال ({maxSessions}) رسیده است."
        };

    public static SessionCreationResult Failed(string error) =>
        new()
        {
            IsSuccess = false,
            Error = error
        };

    public Result<UserSession> ToResult() => IsSuccess
        ? Result<UserSession>.Success(Session!)
        : Result<UserSession>.Failure(new Error("SessionCreation.Failed", Error ?? string.Empty));
}