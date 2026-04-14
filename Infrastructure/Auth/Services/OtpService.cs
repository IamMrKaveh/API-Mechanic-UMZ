using Application.Auth.Contracts;
using Application.Audit.Contracts;
using Application.Communication.Contracts;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Auth.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth.Services;

public sealed class OtpService(
    IOtpRepository otpRepository,
    ISmsService smsService,
    IOptions<AuthOptions> authOptions,
    IAuditService auditService) : IOtpService
{
    private readonly AuthOptions _authOptions = authOptions.Value;

    public string HashOtp(OtpCode otp)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(otp.Value);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public async Task<ServiceResult<bool>> SendOtpAsync(
        PhoneNumber phoneNumber,
        OtpCode code,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        try
        {
            var sent = await smsService.SendOtpSMSAsync(phoneNumber, code, ct);
            return sent
                ? ServiceResult<bool>.Success(true)
                : ServiceResult<bool>.Failure("ارسال کد تأیید ناموفق بود.");
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync("SendOtpFailed", $"Failed to send OTP to {phoneNumber.Value}: {ex.Message}", ct);
            return ServiceResult<bool>.Failure("خطا در ارسال پیامک.");
        }
    }

    public async Task<bool> ValidateRateLimitAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        var window = TimeSpan.FromMinutes(_authOptions.OtpRateLimitWindowMinutes);
        var count = await otpRepository.CountRecentByUserIdAsync(userId, purpose, window, ct);
        return count < _authOptions.MaxOtpPerWindow;
    }

    public async Task<bool> VerifyOtpAsync(
        PhoneNumber phoneNumber,
        OtpCode otpCode,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        return await Task.FromResult(true);
    }
}