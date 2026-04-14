namespace Application.Security.Contracts;

public interface IRateLimitService
{
    Task<(bool IsLimited, TimeSpan? RetryAfterSeconds)> IsLimitedAsync(
        string key,
        int maxAttempts = 5,
        int windowMinutes = 15);
}