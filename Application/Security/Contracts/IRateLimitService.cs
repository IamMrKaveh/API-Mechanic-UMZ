namespace Application.Security.Contracts;

/// <summary>
/// سرویس محدودیت نرخ
/// </summary>
public interface IRateLimitService
{
    Task<(bool IsLimited, int RetryAfterSeconds)> IsLimitedAsync(
        string key,
        int maxAttempts = 5,
        int windowMinutes = 15);
}