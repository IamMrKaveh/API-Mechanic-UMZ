namespace Domain.Security.Enums;

public enum OtpRateLimitStatus
{
    Allowed = 0,
    Blocked = 1,
    TemporarilyBlocked = 2
}