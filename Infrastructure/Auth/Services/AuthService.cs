using Application.Audit.Contracts;
using Application.Auth.Contracts;
using Application.Common.Results;
using Application.Communication.Contracts;
using Application.Security.Contracts;
using Application.User.Features.Shared;
using Domain.Common.Interfaces;
using Domain.Security.Interfaces;
using Domain.User.Interfaces;
using Infrastructure.Auth.Options;
using MapsterMapper;

namespace Infrastructure.Auth.Services;

public sealed class AuthService(
    IOptions<AuthOptions> authOptions,
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    ITokenService tokenService,
    IOtpService otpService,
    ISmsService smsService,
    IRateLimitService rateLimitService,
    IAuditService auditService,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly AuthOptions _authOptions = authOptions.Value;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IOtpService _otpService = otpService;
    private readonly ISmsService _smsService = smsService;
    private readonly IRateLimitService _rateLimitService = rateLimitService;
    private readonly IAuditService _auditService = auditService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<AuthService> _logger = logger;

    private int OtpExpirationMinutes => _authOptions.OtpExpirationMinutes;
    private int MaxOtpAttempts => _authOptions.MaxOtpAttempts;
    private int LockoutDurationMinutes => _authOptions.LockoutDurationMinutes;
    private int RefreshTokenExpirationDays => _authOptions.RefreshTokenExpirationDays;

    public async Task<ServiceResult> RequestOtpAsync(
        string phoneNumber,
        string ipAddress,
        CancellationToken ct = default)
    {
        try
        {
            var (phoneSuccess, phone, phoneError) = PhoneNumber.TryCreate(phoneNumber);
            if (!phoneSuccess)
                return ServiceResult.Unexpected(phoneError!);

            var normalizedPhone = phone!.Value;

            var (isIpLimited, ipRetryAfter) = await _rateLimitService.IsLimitedAsync(
                $"otp_request:ip:{ipAddress}", 10, 60);
            if (isIpLimited)
            {
                _logger.LogWarning("درخواست OTP از IP {IpAddress} مسدود شد.", ipAddress);
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
                    return ServiceResult.RateLimitExceeded($"حساب شما قفل شده است. لطفاً {(int)remaining.Value.TotalMinutes} دقیقه دیگر تلاش کنید.");
            }

            var (canSend, rateLimitError, _) = user.CheckOtpRateLimit();
            if (!canSend)
                return ServiceResult.RateLimitExceeded(rateLimitError!);

            var otpCode = _otpService.GenerateSecureOtp();
            var otpHash = _otpService.HashOtp(otpCode);

            user.GenerateOtp(otpHash, OtpExpirationMinutes);

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
                ipAddress,
                user.Id);

            _logger.LogInformation("کد OTP به {PhoneNumber} ارسال شد.", normalizedPhone);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در درخواست OTP برای شماره {PhoneNumber}", phoneNumber);
            return ServiceResult.Unexpected("خطای داخلی سرور.");
        }
    }

    public async Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>>
        VerifyOtpAsync(
        string phoneNumber,
        string code,
        string ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        try
        {
            var (phoneSuccess, phone, phoneError) = PhoneNumber.TryCreate(phoneNumber);
            if (!phoneSuccess)
                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .Unexpected(phoneError!);

            var normalizedPhone = phone!.Value;

            if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .Unexpected("کد تأیید نامعتبر است.");

            var (isLimited, retryAfter) = await _rateLimitService.IsLimitedAsync(
                $"otp_verify:{normalizedPhone}", MaxOtpAttempts, LockoutDurationMinutes);
            if (isLimited)
                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .RateLimitExceeded($"تعداد تلاش‌های ناموفق بیش از حد مجاز است. لطفاً {retryAfter / 60} دقیقه صبر کنید.");

            var user = await _userRepository.GetWithOtpsAndSessionsByPhoneAsync(normalizedPhone, ct);
            if (user == null)
                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .NotFound("کاربر یافت نشد.");

            if (user.IsLockedOut)
            {
                var remaining = user.GetRemainingLockoutTime();
                if (remaining.HasValue && remaining.Value > TimeSpan.Zero)
                    return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                        .Forbidden($"حساب شما قفل شده است. لطفاً {(int)remaining.Value.TotalMinutes} دقیقه دیگر تلاش کنید.");
                user.ResetLockout();
            }

            var verified = user.VerifyOtp(code);

            if (!verified)
            {
                await _unitOfWork.SaveChangesAsync(ct);

                await _auditService.LogSecurityEventAsync(
                    "OtpVerificationFailed",
                    $"تلاش ناموفق برای تأیید OTP شماره {normalizedPhone}",
                    ipAddress,
                    user.Id);

                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .Unexpected("کد تأیید نادرست یا منقضی شده است.");
            }

            var refreshTokenResult = _tokenService.GenerateRefreshToken();
            var verifierHash = _tokenService.HashToken(refreshTokenResult.Verifier);

            user.CreateSession(
                refreshTokenResult.Selector,
                verifierHash,
                ipAddress,
                userAgent,
                "OtpLogin",
                RefreshTokenExpirationDays);

            await _unitOfWork.SaveChangesAsync(ct);

            var accessToken = _tokenService.GenerateJwtToken(user);
            var isNewUser = user.CreatedAt > DateTime.UtcNow.AddMinutes(-5);

            await _auditService.LogSecurityEventAsync(
                "UserLoggedIn",
                $"ورود موفق کاربر {normalizedPhone}",
                ipAddress,
                user.Id,
                userAgent);

            _logger.LogInformation("کاربر {UserId} با موفقیت وارد شد.", user.Id);

            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                .Success((
                accessToken,
                refreshTokenResult,
                _mapper.Map<UserProfileDto>(user),
                isNewUser));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تأیید OTP برای شماره {PhoneNumber}", phoneNumber);
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                .Unexpected("خطای داخلی سرور.");
        }
    }

    public async Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>>
        RefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        try
        {
            var (selector, verifier) = _tokenService.ParseRefreshToken(refreshToken);
            if (selector == null || verifier == null)
                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .Unexpected("فرمت توکن نامعتبر است.");

            var session = await _sessionRepository.GetBySelectorAsync(selector, ct);
            if (session == null)
                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .Unauthorized("نشست یافت نشد. لطفاً دوباره وارد شوید.");

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                session.Revoke();
                await _unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .Unauthorized("نشست منقضی شده است. لطفاً دوباره وارد شوید.");
            }

            if (session.RevokedAt.HasValue)
            {
                await _auditService.LogSecurityEventAsync(
                    "RevokedTokenReuse",
                    $"تلاش برای استفاده از توکن ابطال شده توسط کاربر {session.UserId}",
                    ipAddress,
                    session.UserId);

                await _sessionRepository.RevokeAllByUserAsync(session.UserId, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .Unauthorized("توکن نامعتبر است. لطفاً دوباره وارد شوید.");
            }

            var verifierHash = _tokenService.HashToken(verifier);
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(session.TokenVerifierHash),
                Encoding.UTF8.GetBytes(verifierHash)))
                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .Unauthorized("توکن نامعتبر است.");

            var user = await _userRepository.GetByIdAsync(session.UserId, ct);
            if (user == null || !user.IsActive || user.IsDeleted)
            {
                session.Revoke();
                await _unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                    .Unauthorized("حساب کاربری غیرفعال است.");
            }

            session.Revoke();
            session.MarkAsReplaced(selector);

            var newRefreshTokenResult = _tokenService.GenerateRefreshToken();
            var newSession = UserSession.Create(
                user.Id,
                newRefreshTokenResult.Selector,
                _tokenService.HashToken(newRefreshTokenResult.Verifier),
                ipAddress,
                userAgent,
                "RefreshToken",
                RefreshTokenExpirationDays);

            await _sessionRepository.AddAsync(newSession, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var accessToken = _tokenService.GenerateJwtToken(user);

            _logger.LogInformation("توکن کاربر {UserId} تمدید شد.", user.Id);

            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                .Success((
                accessToken,
                newRefreshTokenResult,
                _mapper.Map<UserProfileDto>(user),
                false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تمدید توکن");
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>
                .Unexpected("خطای داخلی سرور.");
        }
    }

    public async Task<ServiceResult> LogoutAsync(
        int userId,
        string refreshToken,
        CancellationToken ct = default)
    {
        try
        {
            var (selector, _) = _tokenService.ParseRefreshToken(refreshToken);
            if (selector == null)
                return ServiceResult.Success();

            var session = await _sessionRepository.GetBySelectorAsync(selector, ct);
            if (session != null && session.UserId == userId)
            {
                session.Revoke();
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("کاربر {UserId} از سیستم خارج شد.", userId);
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در خروج کاربر {UserId}", userId);
            return ServiceResult.Unexpected("خطای داخلی سرور.");
        }
    }

    public async Task<ServiceResult> LogoutAllAsync(
        int userId,
        CancellationToken ct = default)
    {
        try
        {
            await _sessionRepository.RevokeAllByUserAsync(userId, ct);
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
            return ServiceResult.Unexpected("خطای داخلی سرور.");
        }
    }
}