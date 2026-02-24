namespace Infrastructure.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;
    private readonly ISmsService _smsService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    private readonly int _otpExpirationMinutes;
    private readonly int _maxOtpAttempts;
    private readonly int _lockoutDurationMinutes;
    private readonly int _refreshTokenExpirationDays = 30;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IOtpService otpService,
        ISmsService smsService,
        IRateLimitService rateLimitService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _otpService = otpService;
        _smsService = smsService;
        _rateLimitService = rateLimitService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;

        _otpExpirationMinutes = configuration.GetValue("Auth:OtpExpirationMinutes", 2);
        _maxOtpAttempts = configuration.GetValue("Auth:MaxOtpAttempts", 5);
        _lockoutDurationMinutes = configuration.GetValue("Auth:LockoutDurationMinutes", 15);
    }

    public async Task<ServiceResult> RequestOtpAsync(
        string phoneNumber,
        string ipAddress,
        CancellationToken ct = default)
    {
        try
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);

            if (!IsValidPhoneNumber(normalizedPhone))
            {
                return ServiceResult.Failure("شماره تلفن نامعتبر است.");
            }

            var ipRateLimitKey = $"otp_request:ip:{ipAddress}";
            var (isIpLimited, ipRetryAfter) = await _rateLimitService.IsLimitedAsync(ipRateLimitKey, 10, 60);
            if (isIpLimited)
            {
                _logger.LogWarning("درخواست OTP از IP {IpAddress} مسدود شد.", ipAddress);
                return ServiceResult.Failure($"تعداد درخواست‌ها بیش از حد مجاز است. لطفاً {ipRetryAfter} ثانیه صبر کنید.", 429);
            }

            var phoneRateLimitKey = $"otp_request:phone:{normalizedPhone}";
            var (isPhoneLimited, phoneRetryAfter) = await _rateLimitService.IsLimitedAsync(phoneRateLimitKey, 3, 2);
            if (isPhoneLimited)
            {
                _logger.LogWarning("درخواست OTP برای شماره {PhoneNumber} مسدود شد.", normalizedPhone);
                return ServiceResult.Failure($"درخواست‌های متعدد. لطفاً {phoneRetryAfter} ثانیه صبر کنید.", 429);
            }

            var user = await _userRepository.GetByPhoneNumberAsync(normalizedPhone);
            var isNewUser = user == null;

            if (isNewUser)
            {
                user = Domain.User.User.Create(normalizedPhone);
                await _userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation("کاربر جدید با شماره {PhoneNumber} ایجاد شد.", normalizedPhone);
            }

            if (user!.IsLockedOut)
            {
                var remainingLockout = user.LockoutEnd!.Value - DateTime.UtcNow;
                if (remainingLockout > TimeSpan.Zero)
                {
                    return ServiceResult.Failure(
                        $"حساب شما به دلیل تلاش‌های ناموفق قفل شده است. لطفاً {(int)remainingLockout.TotalMinutes} دقیقه دیگر تلاش کنید.");
                }
            }

            await _userRepository.DeleteUserOtpsAsync(user.Id);

            var otpCode = _otpService.GenerateSecureOtp();
            var otpHash = _otpService.HashOtp(otpCode);

            var userOtp = UserOtp.Create(user.Id, otpHash, _otpExpirationMinutes);

            await _userRepository.AddUserOtpAsync(userOtp);
            await _unitOfWork.SaveChangesAsync(ct);

            var smsResult = await _smsService.SendSmsAsync(normalizedPhone, otpCode, ct);

            if (smsResult.IsFailed)
            {
                _logger.LogError("ارسال OTP به {PhoneNumber} ناموفق بود: {Error}", normalizedPhone, smsResult.ErrorMessage);
                return ServiceResult.Failure("خطا در ارسال کد تأیید. لطفاً دوباره تلاش کنید.");
            }

            await _auditService.LogSecurityEventAsync(
                "OtpRequested",
                $"کد OTP برای شماره {normalizedPhone} ارسال شد.",
                ipAddress,
                user.Id);

            _logger.LogInformation("کد OTP به {PhoneNumber} ارسال شد.", normalizedPhone);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در درخواست OTP برای شماره {PhoneNumber}", phoneNumber);
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }

    public async Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, Domain.User.User User, bool IsNewUser)>>
        VerifyOtpAsync(
        string phoneNumber,
        string code,
        string ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        try
        {
            var normalizedPhone = NormalizePhoneNumber(phoneNumber);

            if (!IsValidPhoneNumber(normalizedPhone))
            {
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("شماره تلفن نامعتبر است.");
            }

            if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            {
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("کد تأیید نامعتبر است.");
            }

            var rateLimitKey = $"otp_verify:{normalizedPhone}";
            var (isLimited, retryAfter) = await _rateLimitService.IsLimitedAsync(rateLimitKey, _maxOtpAttempts, _lockoutDurationMinutes);
            if (isLimited)
            {
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure(
                    $"تعداد تلاش‌های ناموفق بیش از حد مجاز است. لطفاً {retryAfter / 60} دقیقه صبر کنید.", 429);
            }

            var user = await _userRepository.GetByPhoneNumberAsync(normalizedPhone);
            if (user == null)
            {
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("کاربر یافت نشد.", 404);
            }

            if (user.IsLockedOut)
            {
                var remainingLockout = user.LockoutEnd!.Value - DateTime.UtcNow;
                if (remainingLockout > TimeSpan.Zero)
                {
                    return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure(
                        $"حساب شما قفل شده است. لطفاً {(int)remainingLockout.TotalMinutes} دقیقه دیگر تلاش کنید.");
                }
                user.ResetLockout();
            }

            var userOtp = await _userRepository.GetActiveOtpAsync(user.Id);
            if (userOtp == null)
            {
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("کد تأیید منقضی شده یا وجود ندارد. لطفاً کد جدید درخواست کنید.");
            }

            if (userOtp.ExpiresAt < DateTime.UtcNow)
            {
                await _userRepository.DeleteUserOtpsAsync(user.Id);
                await _unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("کد تأیید منقضی شده است. لطفاً کد جدید درخواست کنید.");
            }

            userOtp.IncrementAttempts();
            await _unitOfWork.SaveChangesAsync(ct);

            if (userOtp.AttemptCount > _maxOtpAttempts)
            {
                user.LockAccount(TimeSpan.FromMinutes(_lockoutDurationMinutes));
                await _userRepository.DeleteUserOtpsAsync(user.Id);
                await _unitOfWork.SaveChangesAsync(ct);

                await _auditService.LogSecurityEventAsync(
                    "AccountLocked",
                    $"حساب کاربر {normalizedPhone} به دلیل تلاش‌های ناموفق قفل شد.",
                    ipAddress,
                    user.Id);

                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("تعداد تلاش‌های ناموفق بیش از حد مجاز. حساب شما موقتاً قفل شد.");
            }

            if (!_otpService.VerifyOtp(code, userOtp.OtpHash))
            {
                await _auditService.LogSecurityEventAsync(
                    "OtpVerificationFailed",
                    $"تلاش ناموفق برای تأیید OTP شماره {normalizedPhone}",
                    ipAddress,
                    user.Id);

                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure($"کد تأیید نادرست است. {_maxOtpAttempts - userOtp.AttemptCount} تلاش باقی مانده.");
            }

            await _userRepository.DeleteUserOtpsAsync(user.Id);
            user.ResetLockout();
            user.RecordSuccessfulLogin();

            var refreshTokenResult = _tokenService.GenerateRefreshToken();

            var session = UserSession.Create(
                       user.Id,
                       refreshTokenResult.Selector,
                       _tokenService.HashToken(refreshTokenResult.Verifier),
                       ipAddress,
                       userAgent,
                       "OtpLogin",
                       30
                   );

            await _userRepository.AddSessionAsync(session);
            await _unitOfWork.SaveChangesAsync(ct);

            var accessToken = _tokenService.GenerateJwtToken(user);

            await _auditService.LogSecurityEventAsync(
                "UserLoggedIn",
                $"ورود موفق کاربر {normalizedPhone}",
                ipAddress,
                user.Id,
                userAgent);

            _logger.LogInformation("کاربر {UserId} با موفقیت وارد شد.", user.Id);

            var isNewUser = user.CreatedAt > DateTime.UtcNow.AddMinutes(-5);

            return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Success((
                accessToken,
                refreshTokenResult,
                user,
                isNewUser
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تأیید OTP برای شماره {PhoneNumber}", phoneNumber);
            return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("خطای داخلی سرور.");
        }
    }

    public async Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, Domain.User.User User, bool IsNewUser)>>
        RefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("توکن نامعتبر است.");
            }

            var parts = refreshToken.Split('.');
            if (parts.Length != 2)
            {
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("فرمت توکن نامعتبر است.");
            }

            var selector = parts[0];
            var verifier = parts[1];

            var session = await _userRepository.GetSessionBySelectorAsync(selector);
            if (session == null)
            {
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("نشست یافت نشد. لطفاً دوباره وارد شوید.", 401);
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                await _userRepository.RevokeSessionAsync(session.Id);
                await _unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("نشست منقضی شده است. لطفاً دوباره وارد شوید.", 401);
            }

            if (session.RevokedAt.HasValue)
            {
                await _auditService.LogSecurityEventAsync(
                    "RevokedTokenReuse",
                    $"تلاش برای استفاده از توکن ابطال شده توسط کاربر {session.UserId}",
                    ipAddress,
                    session.UserId);

                await _userRepository.RevokeAllUserSessionsAsync(session.UserId);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("توکن نامعتبر است. لطفاً دوباره وارد شوید.", 401);
            }

            var verifierHash = _tokenService.HashToken(verifier);
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(session.TokenVerifierHash),
                Encoding.UTF8.GetBytes(verifierHash)))
            {
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("توکن نامعتبر است.", 401);
            }

            var user = await _userRepository.GetByIdAsync(session.UserId);
            if (user == null || !user.IsActive || user.IsDeleted)
            {
                await _userRepository.RevokeSessionAsync(session.Id);
                await _unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("حساب کاربری غیرفعال است.", 401);
            }

            await _userRepository.RevokeSessionAsync(session.Id);

            var newRefreshTokenResult = _tokenService.GenerateRefreshToken();

            var newSession = UserSession.Create(
                 user.Id,
                 newRefreshTokenResult.Selector,
                 _tokenService.HashToken(newRefreshTokenResult.Verifier),
                 ipAddress,
                 userAgent,
                 "RefreshToken",
                 _refreshTokenExpirationDays
             );

            session.MarkAsReplaced(newRefreshTokenResult.Selector);

            await _userRepository.AddSessionAsync(newSession);
            await _unitOfWork.SaveChangesAsync(ct);

            var accessToken = _tokenService.GenerateJwtToken(user);
            var isNewUser = false;

            _logger.LogInformation("توکن کاربر {UserId} تمدید شد.", user.Id);

            return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Success((
             accessToken,
             newRefreshTokenResult,
             user,
             isNewUser
         ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تمدید توکن");
            return ServiceResult<(string, RefreshTokenResult, Domain.User.User, bool)>.Failure("خطای داخلی سرور.");
        }
    }

    public async Task<ServiceResult> LogoutAsync(
        int userId,
        string refreshToken,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return ServiceResult.Success();
            }

            var parts = refreshToken.Split('.');
            if (parts.Length != 2)
            {
                return ServiceResult.Success();
            }

            var selector = parts[0];

            var session = await _userRepository.GetSessionBySelectorAsync(selector);
            if (session != null && session.UserId == userId)
            {
                await _userRepository.RevokeSessionAsync(session.Id);
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation("کاربر {UserId} از سیستم خارج شد.", userId);
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در خروج کاربر {UserId}", userId);
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }

    public async Task<ServiceResult> LogoutAllAsync(
        int userId,
        CancellationToken ct = default)
    {
        try
        {
            await _userRepository.RevokeAllUserSessionsAsync(userId);
            await _unitOfWork.SaveChangesAsync(ct);

            await _auditService.LogSecurityEventAsync(
                "LogoutAll",
                $"کاربر {userId} از تمام دستگاه‌ها خارج شد.",
                "system",
                userId);

            _logger.LogInformation("کاربر {UserId} از تمام دستگاه‌ها خارج شد.", userId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در خروج کاربر {UserId} از تمام دستگاه‌ها", userId);
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var normalized = phoneNumber.Trim()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("+98", "0")
            .Replace("0098", "0");

        if (!normalized.StartsWith("0") && normalized.Length == 10)
        {
            normalized = "0" + normalized;
        }

        return normalized;
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        if (phoneNumber.Length != 11)
            return false;

        if (!phoneNumber.StartsWith("09"))
            return false;

        return phoneNumber.All(char.IsDigit);
    }
}