namespace MainApi.Services;

public class RateLimitService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _maxAttempts = 5;
    private readonly TimeSpan _window = TimeSpan.FromMinutes(2);

    public RateLimitService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> IsLimitedAsync(string phoneNumber)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MechanicContext>();

        var rate = await context.TRateLimits.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        if (rate == null) return false;

        if (DateTime.UtcNow - rate.LastAttempt > _window)
        {
            rate.Attempts = 0;
            await context.SaveChangesAsync();
            return false;
        }

        return rate.Attempts >= _maxAttempts;
    }

    public async Task RegisterAttemptAsync(string phoneNumber)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MechanicContext>();

        var rate = await context.TRateLimits.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);

        if (rate == null)
        {
            rate = new TRateLimits
            {
                PhoneNumber = phoneNumber,
                Attempts = 1,
                LastAttempt = DateTime.UtcNow
            };
            context.TRateLimits.Add(rate);
        }
        else
        {
            if (DateTime.UtcNow - rate.LastAttempt > _window)
                rate.Attempts = 1;
            else
                rate.Attempts++;

            rate.LastAttempt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }
}