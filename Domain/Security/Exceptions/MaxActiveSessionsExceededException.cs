using Domain.Common.Exceptions;
using Domain.User.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class MaxActiveSessionsExceededException : DomainException
{
    public UserId UserId { get; }
    public int MaxSessions { get; }

    public override string ErrorCode => "MAX_ACTIVE_SESSIONS_EXCEEDED";

    public MaxActiveSessionsExceededException(UserId userId, int maxSessions)
        : base($"کاربر '{userId}' به حداکثر تعداد نشست‌های فعال ({maxSessions}) رسیده است.")
    {
        UserId = userId;
        MaxSessions = maxSessions;
    }
}