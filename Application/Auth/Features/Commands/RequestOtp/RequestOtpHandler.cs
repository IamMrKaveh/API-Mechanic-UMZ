using Application.Audit.Contracts;
using Application.Auth.Contracts;
using Application.Common.Results;
using Application.Communication.Contracts;
using Application.Security.Contracts;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.RequestOtp;

public class RequestOtpHandler(
    IUserRepository userRepository,
    IOtpService otpService,
    ISmsService smsService,
    IRateLimitService rateLimitService,
    IAuditService auditService,
    IUnitOfWork unitOfWork,
    ILogger<RequestOtpHandler> logger) : IRequestHandler<RequestOtpCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IOtpService _otpService = otpService;
    private readonly ISmsService _smsService = smsService;
    private readonly IRateLimitService _rateLimitService = rateLimitService;
    private readonly IAuditService _auditService = auditService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<RequestOtpHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        RequestOtpCommand request,
        CancellationToken ct)
    {
        try
        {
            var (phoneSuccess, phoneNumber, phoneError) = PhoneNumber.TryCreate(request.PhoneNumber);
            if (!phoneSuccess)
                return ServiceResult.Unexpected(phoneError!);

            var normalizedPhone = phoneNumber!.Value;

            var (isIpLimited, ipRetryAfter) = await _rateLimitService.IsLimitedAsync(
                $"otp_request:ip:{request.IpAddress}", 10, 60);
            if (isIpLimited)
            {
                _logger.LogWarning("درخواست OTP از IP {IpAddress} مسدود شد.", request.IpAddress);
                return ServiceResult.RateLimitExceeded($"تعداد درخواست‌ها بیش از حد مجاز است. لطفاً {ipRetryAfter} ثانیه صبر کنید.");
            }

            var (isPhoneLimited, phoneRetryAfter) = await _rateLimitService.IsLimitedAsync(
                $"otp_request:phone:{normalizedPhone}", 3, 2);
            if (isPhoneLimited)
            {
                _logger.LogWarning("درخواست OTP برای شماره {PhoneNumber} مسدود شد.", normalizedPhone);
                return ServiceResult.RateLimitExceeded($"درخواست‌های متعدد. لطفاً {phoneRetryAfter} ثانیه صبر کنید.");
            }

            var user = await _userRepository.GetWithOtpsByPhoneAsync(normalizedPhone, ct);

            if (user == null)
            {
                user = Domain.User.Aggregates.User.Create(normalizedPhone);
                await _userRepository.AddAsync(user, ct);
            }

            if (user.IsLockedOut)
            {
                var remaining = user.GetRemainingLockoutTime();
                if (remaining.HasValue && remaining.Value > TimeSpan.Zero)
                    return ServiceResult.Unexpected(
                        $"حساب شما قفل شده است. لطفاً {(int)remaining.Value.TotalMinutes} دقیقه دیگر تلاش کنید.");
            }

            var (canSend, rateLimitError, _) = user.CheckOtpRateLimit();
            if (!canSend)
                return ServiceResult.RateLimitReached(rateLimitError!);

            var otpCode = _otpService.GenerateSecureOtp();
            var otpHash = _otpService.HashOtp(otpCode);

            user.GenerateOtp(otpHash);

            await _unitOfWork.SaveChangesAsync(ct);

            var smsResult = await _smsService.SendSmsAsync(normalizedPhone, otpCode, ct);
            if (smsResult.IsFailed)
            {
                _logger.LogError("ارسال OTP به {PhoneNumber} ناموفق بود: {Error}",
                    normalizedPhone, smsResult.ErrorMessage);
                return ServiceResult.Unexpected("خطا در ارسال کد تأیید. لطفاً دوباره تلاش کنید.");
            }

            await _auditService.LogSecurityEventAsync(
                "OtpRequested",
                $"کد OTP برای شماره {normalizedPhone} ارسال شد.",
                request.IpAddress,
                user.Id);

            _logger.LogInformation("کد OTP به {PhoneNumber} ارسال شد.", normalizedPhone);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در درخواست OTP برای شماره {PhoneNumber}", request.PhoneNumber);
            return ServiceResult.Unexpected("خطای داخلی سرور.");
        }
    }
}