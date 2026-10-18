using System.Security.Cryptography;
using System.Text;
using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Auth.Services;

public sealed class OtpService : IOtpService
{
    private const int MinPepperLength = 16;

    private readonly IOtpRepository _otpRepository;
    private readonly ISmsService _smsService;
    private readonly IAuditService _auditService;
    private readonly AuthOptions _authOptions;
    private readonly byte[] _pepper;

    public OtpService(
        IOtpRepository otpRepository,
        ISmsService smsService,
        IOptions<AuthOptions> authOptions,
        IAuditService auditService)
    {
        _otpRepository = otpRepository;
        _smsService = smsService;
        _auditService = auditService;
        _authOptions = authOptions.Value;

        var pepper = _authOptions.OtpPepper;
        if (string.IsNullOrWhiteSpace(pepper) || pepper.Length < MinPepperLength)
        {
            throw new InvalidOperationException(
                $"Auth:OtpPepper must be configured with at least {MinPepperLength} characters " +
                "(generate one with: openssl rand -base64 48).");
        }

        _pepper = Encoding.UTF8.GetBytes(pepper);
    }

    public string HashOtp(OtpCode otp)
    {
        // Keyed hash: a database leak alone must not allow offline cracking of
        // 6-digit codes (unsalted SHA-256 has only ~10^6 candidates).
        using var hmac = new HMACSHA256(_pepper);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(otp.Value));
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
            var sent = await _smsService.SendOtpSMSAsync(phoneNumber, code, ct);
            return sent
                ? ServiceResult<bool>.Success(true)
                : ServiceResult<bool>.Failure("ارسال کد تأیید ناموفق بود.");
        }
        catch (Exception ex)
        {
            await _auditService.LogSystemEventAsync("SendOtpFailed", $"Failed to send OTP to {phoneNumber.Value}: {ex.Message}", ct);
            return ServiceResult<bool>.Failure("خطا در ارسال پیامک.");
        }
    }

    public async Task<bool> ValidateRateLimitAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        var window = TimeSpan.FromMinutes(_authOptions.OtpRateLimitWindowMinutes);
        var count = await _otpRepository.CountRecentByUserIdAsync(userId, purpose, window, ct);
        return count < _authOptions.MaxOtpPerWindow;
    }
}
