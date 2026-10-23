using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Auth.Repositories;

public sealed class OtpRepository(DBContext context, IDateTimeProvider dateTimeProvider)
    : RepositoryBase<UserOtp, OtpId>(context), IOtpRepository
{
    public async Task<UserOtp?> GetLatestActiveByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        var now = dateTimeProvider.UtcNow;
        return await Context.UserOtps
            .Where(o => o.UserId == userId
                     && o.Purpose == purpose
                     && !o.IsVerified
                     && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CountRecentByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        TimeSpan window,
        CancellationToken ct = default)
    {
        var since = dateTimeProvider.UtcNow - window;
        return await Context.UserOtps
            .CountAsync(o => o.UserId == userId
                          && o.Purpose == purpose
                          && o.CreatedAt >= since, ct);
    }

    public async Task InvalidateAllActiveByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        // Set-based invalidation: expiry is pushed forward to now for every active
        // OTP of the user in a single statement. The previous load-then-MarkExpired
        // loop was a no-op (MarkExpired only raises an event for already-expired OTPs).
        var now = dateTimeProvider.UtcNow;
        await Context.UserOtps
            .Where(o => o.UserId == userId
                     && o.Purpose == purpose
                     && !o.IsVerified
                     && o.ExpiresAt > now)
            .ExecuteUpdateAsync(u => u
                .SetProperty(o => o.ExpiresAt, now), ct);
    }
}