using Domain.Common.Interfaces;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Interfaces;

public interface IOtpRepository : IRepository<UserOtp, OtpId>
{
    Task<UserOtp?> GetLatestActiveByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default);

    Task<int> CountRecentByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        TimeSpan window,
        CancellationToken ct = default);

    Task InvalidateAllActiveByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default);
}