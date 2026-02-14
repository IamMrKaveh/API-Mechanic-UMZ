using Application.Audit.Contracts;
using Application.Auth.Features.Shared;
using Application.Security.Contracts;

namespace Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpHandler : IRequestHandler<VerifyOtpCommand, ServiceResult<AuthResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;
    private readonly ISessionService _sessionManager;
    private readonly IRateLimitService _rateLimitService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VerifyOtpHandler> _logger;

    private const int MaxOtpAttempts = 5;
    private const int LockoutDurationMinutes = 15;

    public VerifyOtpHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        IOtpService otpService,
        ISessionService sessionManager,
        IRateLimitService rateLimitService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<VerifyOtpHandler> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _otpService = otpService;
        _sessionManager = sessionManager;
        _rateLimitService = rateLimitService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthResult>> Handle(
        VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. اعتبارسنجی شماره تلفن
            var (phoneSuccess, phoneNumber, phoneError) =
                PhoneNumber.TryCreate(request.PhoneNumber);
            if (!phoneSuccess)
                return ServiceResult<AuthResult>.Failure(phoneError!);

            var normalizedPhone = phoneNumber!.Value;

            // 2. بررسی Rate Limit
            var (isLimited, retryAfter) = await _rateLimitService.IsLimitedAsync(
                $"otp_verify:{normalizedPhone}", MaxOtpAttempts, LockoutDurationMinutes);
            if (isLimited)
            {
                return ServiceResult<AuthResult>.Failure(
                    $"تعداد تلاش‌های ناموفق بیش از حد مجاز است. لطفاً {retryAfter / 60} دقیقه صبر کنید.", 429);
            }

            // 3. دریافت کاربر با OTPها (برای Domain Logic)
            var user = await _userRepository.GetWithOtpsAsync(
                (await _userRepository.GetByPhoneNumberAsync(normalizedPhone, cancellationToken))?.Id ?? 0,
                cancellationToken);

            if (user == null)
                return ServiceResult<AuthResult>.Failure("کاربر یافت نشد.", 404);

            // 4. بررسی قابلیت لاگین (Domain Logic)
            if (!user.CanLogin())
            {
                if (user.IsLockedOut)
                {
                    var remaining = user.GetRemainingLockoutTime();
                    return ServiceResult<AuthResult>.Failure(
                        $"حساب شما قفل شده است. لطفاً {(int)(remaining?.TotalMinutes ?? LockoutDurationMinutes)} دقیقه دیگر تلاش کنید.");
                }
                return ServiceResult<AuthResult>.Failure("حساب کاربری غیرفعال است.");
            }

            // 5. تأیید OTP (Domain Logic)
            var otpHash = _otpService.HashOtp(request.Code);
            var isOtpValid = user.VerifyOtp(request.Code);

            // ذخیره تغییرات Domain (شمارنده تلاش، قفل احتمالی)
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!isOtpValid)
            {
                await _auditService.LogSecurityEventAsync(
                    "OtpVerificationFailed",
                    $"تلاش ناموفق برای تأیید OTP شماره {normalizedPhone}",
                    request.IpAddress,
                    user.Id);

                return ServiceResult<AuthResult>.Failure("کد تأیید نادرست است.");
            }

            // 6. OTP صحیح - ایجاد Session
            var refreshTokenResult = _tokenService.GenerateRefreshToken();
            var verifierHash = _tokenService.HashToken(refreshTokenResult.Verifier);

            var sessionInfo = await _sessionManager.CreateSessionAsync(
                user.Id,
                refreshTokenResult.Selector,
                verifierHash,
                request.IpAddress,
                request.UserAgent,
                "OtpLogin",
                30,
                cancellationToken);

            // 7. ثبت لاگین موفق در Domain
            user.RecordSuccessfulLogin();
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 8. تولید Access Token
            var accessToken = _tokenService.GenerateJwtToken(user);

            // 9. ثبت لاگ
            await _auditService.LogSecurityEventAsync(
                "UserLoggedIn",
                $"ورود موفق کاربر {normalizedPhone}",
                request.IpAddress,
                user.Id,
                request.UserAgent);

            _logger.LogInformation("کاربر {UserId} با موفقیت وارد شد.", user.Id);

            var isNewUser = user.CreatedAt > DateTime.UtcNow.AddMinutes(-5);

            return ServiceResult<AuthResult>.Success(new AuthResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenResult.FullToken,
                AccessTokenExpiresAt = _tokenService.GetAccessTokenExpiration(),
                RefreshTokenExpiresAt = sessionInfo.ExpiresAt,
                User = _mapper.Map<UserProfileDto>(user),
                IsNewUser = isNewUser
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تأیید OTP برای شماره {PhoneNumber}", request.PhoneNumber);
            return ServiceResult<AuthResult>.Failure("خطای داخلی سرور.");
        }
    }
}