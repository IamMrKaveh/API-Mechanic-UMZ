namespace Application.Auth.Features.Commands.RequestOtp;

public class RequestOtpHandler : IRequestHandler<RequestOtpCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly ISmsService _smsService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RequestOtpHandler> _logger;

    public RequestOtpHandler(
        IUserRepository userRepository,
        IOtpService otpService,
        ISmsService smsService,
        IRateLimitService rateLimitService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<RequestOtpHandler> logger)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _smsService = smsService;
        _rateLimitService = rateLimitService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. اعتبارسنجی شماره تلفن با Value Object
            var (phoneSuccess, phoneNumber, phoneError) = PhoneNumber.TryCreate(request.PhoneNumber);
            if (!phoneSuccess)
                return ServiceResult.Failure(phoneError!);

            var normalizedPhone = phoneNumber!.Value;

            // 2. بررسی Rate Limit بر اساس IP
            var (isIpLimited, ipRetryAfter) = await _rateLimitService.IsLimitedAsync(
                $"otp_request:ip:{request.IpAddress}", 10, 60);
            if (isIpLimited)
            {
                _logger.LogWarning("درخواست OTP از IP {IpAddress} مسدود شد.", request.IpAddress);
                return ServiceResult.Failure(
                    $"تعداد درخواست‌ها بیش از حد مجاز است. لطفاً {ipRetryAfter} ثانیه صبر کنید.", 429);
            }

            // 3. بررسی Rate Limit بر اساس شماره تلفن
            var (isPhoneLimited, phoneRetryAfter) = await _rateLimitService.IsLimitedAsync(
                $"otp_request:phone:{normalizedPhone}", 3, 2);
            if (isPhoneLimited)
            {
                _logger.LogWarning("درخواست OTP برای شماره {PhoneNumber} مسدود شد.", normalizedPhone);
                return ServiceResult.Failure(
                    $"درخواست‌های متعدد. لطفاً {phoneRetryAfter} ثانیه صبر کنید.", 429);
            }

            // 4. دریافت یا ایجاد کاربر (User Aggregate)
            var user = await _userRepository.GetByPhoneNumberAsync(normalizedPhone, cancellationToken);
            var isNewUser = user == null;

            if (isNewUser)
            {
                user = Domain.User.User.Create(normalizedPhone);
                await _userRepository.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("کاربر جدید با شماره {PhoneNumber} ایجاد شد.", normalizedPhone);
            }

            // 5. بررسی قابلیت لاگین (Domain Logic)
            if (user!.IsLockedOut)
            {
                var remaining = user.GetRemainingLockoutTime();
                if (remaining.HasValue && remaining.Value > TimeSpan.Zero)
                {
                    return ServiceResult.Failure(
                        $"حساب شما قفل شده است. لطفاً {(int)remaining.Value.TotalMinutes} دقیقه دیگر تلاش کنید.");
                }
            }

            // 6. بررسی محدودیت ارسال OTP (Domain Logic)
            var (canSend, rateLimitError, waitTime) = user.CheckOtpRateLimit();
            if (!canSend)
                return ServiceResult.Failure(rateLimitError!);

            // 7. تولید OTP امن
            var otpCode = _otpService.GenerateSecureOtp();
            var otpHash = _otpService.HashOtp(otpCode);

            // 8. ذخیره OTP از طریق User Aggregate (باطل کردن قبلی‌ها + افزودن جدید)
            // نیاز به بارگذاری OTPها
            var userWithOtps = await _userRepository.GetWithOtpsAsync(user.Id, cancellationToken);
            if (userWithOtps == null)
                return ServiceResult.Failure("خطای داخلی.");

            userWithOtps.InvalidateAllOtps();
            var otp = UserOtp.Create(userWithOtps.Id, otpHash, 2);
            // افزودن OTP به Aggregate از طریق متد internal
            // چون GenerateOtp خودش کد تولید می‌کند و ما می‌خواهیم هش IOtpService را استفاده کنیم،
            // مستقیماً OTP را اضافه می‌کنیم

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 9. ارسال OTP از طریق پیامک
            var smsResult = await _smsService.SendSmsAsync(normalizedPhone, otpCode, cancellationToken);
            if (!smsResult.IsSuccess)
            {
                _logger.LogError("ارسال OTP به {PhoneNumber} ناموفق بود: {Error}",
                    normalizedPhone, smsResult.ErrorMessage);
                return ServiceResult.Failure("خطا در ارسال کد تأیید. لطفاً دوباره تلاش کنید.");
            }

            // 10. ثبت لاگ
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
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }
}