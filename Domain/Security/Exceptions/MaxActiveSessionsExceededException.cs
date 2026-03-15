using Domain.User.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class MaxActiveSessionsExceededException(UserId userId, int maxSessions)
    : DomainException($"کاربر '{userId}' به حداکثر تعداد نشست‌های فعال ({maxSessions}) رسیده است.")
{
    public UserId UserId { get; } = userId;
    public int MaxSessions { get; } = maxSessions;
}