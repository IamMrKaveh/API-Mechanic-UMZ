namespace Application.Security.Contracts;

public interface IRateLimitService
{
    Task<(bool IsLimited, int RetryAfterSeconds)> IsLimitedAsync(
        string key,
        int maxAttempts = 5,
        int windowMinutes = 15);
}