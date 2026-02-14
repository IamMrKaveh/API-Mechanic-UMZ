using Application.Audit.Contracts;
using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, ServiceResult<AuthResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessionManager;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        ISessionService sessionManager,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RefreshTokenHandler> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _sessionManager = sessionManager;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthResult>> Handle(
        RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. تجزیه Refresh Token
            var (selector, verifier) = _tokenService.ParseRefreshToken(request.RefreshToken);
            if (selector == null || verifier == null)
                return ServiceResult<AuthResult>.Failure("فرمت توکن نامعتبر است.", 401);

            // 2. یافتن Session
            var session = await _sessionManager.GetSessionBySelectorAsync(selector, cancellationToken);
            if (session == null)
                return ServiceResult<AuthResult>.Failure("نشست یافت نشد. لطفاً دوباره وارد شوید.", 401);

            // 3. بررسی انقضا
            if (session.IsExpired)
            {
                await _sessionManager.RevokeSessionAsync(session.Id, cancellationToken);
                return ServiceResult<AuthResult>.Failure("نشست منقضی شده است. لطفاً دوباره وارد شوید.", 401);
            }

            // 4. بررسی ابطال (Token Reuse Detection)
            if (session.IsRevoked)
            {
                await _auditService.LogSecurityEventAsync(
                    "RevokedTokenReuse",
                    $"تلاش برای استفاده از توکن ابطال شده توسط کاربر {session.UserId}",
                    request.IpAddress,
                    session.UserId);

                // ابطال تمام Sessionهای کاربر (امنیتی)
                await _sessionManager.RevokeAllUserSessionsAsync(session.UserId, cancellationToken);
                return ServiceResult<AuthResult>.Failure("توکن نامعتبر است. لطفاً دوباره وارد شوید.", 401);
            }

            // 5. تأیید Verifier
            var verifierHash = _tokenService.HashToken(verifier);
            if (!CryptographicEquals(session.TokenVerifierHash, verifierHash))
                return ServiceResult<AuthResult>.Failure("توکن نامعتبر است.", 401);

            // 6. دریافت کاربر
            var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
            if (user == null || !user.IsActive || user.IsDeleted)
            {
                await _sessionManager.RevokeSessionAsync(session.Id, cancellationToken);
                return ServiceResult<AuthResult>.Failure("حساب کاربری غیرفعال است.", 401);
            }

            // 7. Token Rotation: ابطال Session قدیمی و ایجاد جدید
            await _sessionManager.RevokeSessionAsync(session.Id, cancellationToken);

            var newRefreshTokenResult = _tokenService.GenerateRefreshToken();
            var newVerifierHash = _tokenService.HashToken(newRefreshTokenResult.Verifier);

            var newSession = await _sessionManager.CreateSessionAsync(
                user.Id,
                newRefreshTokenResult.Selector,
                newVerifierHash,
                request.IpAddress,
                request.UserAgent,
                "RefreshToken",
                30,
                cancellationToken);

            // 8. تولید Access Token جدید
            var accessToken = _tokenService.GenerateJwtToken(user);

            _logger.LogInformation("توکن کاربر {UserId} تمدید شد.", user.Id);

            return ServiceResult<AuthResult>.Success(new AuthResult
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshTokenResult.FullToken,
                AccessTokenExpiresAt = _tokenService.GetAccessTokenExpiration(),
                RefreshTokenExpiresAt = newSession.ExpiresAt,
                User = _mapper.Map<UserProfileDto>(user),
                IsNewUser = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تمدید توکن");
            return ServiceResult<AuthResult>.Failure("خطای داخلی سرور.");
        }
    }

    private static bool CryptographicEquals(string a, string b)
    {
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(a),
            System.Text.Encoding.UTF8.GetBytes(b));
    }
}