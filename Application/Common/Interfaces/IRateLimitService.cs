namespace Application.Common.Interfaces;

public interface IRateLimitService
{
    Task<bool> IsLimitedAsync(string key, int maxAttempts = 5, int windowMinutes = 15);
}