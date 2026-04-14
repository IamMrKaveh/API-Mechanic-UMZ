using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Auth.Repositories;

public sealed class OtpRepository(DBContext context) : IOtpRepository
{
    public async Task<UserOtp?> GetByIdAsync(OtpId otpId, CancellationToken ct = default)
    {
        return await context.UserOtps.FirstOrDefaultAsync(o => o.Id == otpId, ct);
    }

    public async Task<UserOtp?> GetLatestActiveByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await context.UserOtps
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
        var since = DateTime.UtcNow - window;
        return await context.UserOtps
            .CountAsync(o => o.UserId == userId
                          && o.Purpose == purpose
                          && o.CreatedAt >= since, ct);
    }

    public async Task AddAsync(UserOtp otp, CancellationToken ct = default)
    {
        await context.UserOtps.AddAsync(otp, ct);
    }

    public void Update(UserOtp otp)
    {
        context.UserOtps.Update(otp);
    }

    public async Task InvalidateAllActiveByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var activeOtps = await context.UserOtps
            .Where(o => o.UserId == userId
                     && o.Purpose == purpose
                     && !o.IsVerified
                     && o.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var otp in activeOtps)
            otp.MarkExpired();
    }
}