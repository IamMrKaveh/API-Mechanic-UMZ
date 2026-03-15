using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Interfaces;

public interface IOtpRepository
{
    Task<UserOtp?> GetByIdAsync(
        UserOtpId otpId,
        CancellationToken ct = default);

    Task<UserOtp?> GetLatestActiveByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default);

    Task<int> CountRecentByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        TimeSpan window,
        CancellationToken ct = default);

    Task AddAsync(
        UserOtp otp,
        CancellationToken ct = default);

    void Update(UserOtp otp);

    Task InvalidateAllActiveByUserIdAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default);
}