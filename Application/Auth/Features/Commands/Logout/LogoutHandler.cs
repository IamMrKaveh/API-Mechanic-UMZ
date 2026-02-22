namespace Application.Auth.Features.Commands.Logout;

public class LogoutHandler : IRequestHandler<LogoutCommand, ServiceResult>
{
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessionManager;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        ITokenService tokenService,
        ISessionService sessionManager,
        ILogger<LogoutHandler> logger
        )
    {
        _tokenService = tokenService;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        LogoutCommand request,
        CancellationToken ct
        )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return ServiceResult.Success();

            var (selector, _) = _tokenService.ParseRefreshToken(request.RefreshToken);
            if (selector == null)
                return ServiceResult.Success();

            var session = await _sessionManager.GetSessionBySelectorAsync(selector, ct);
            if (session != null && session.UserId == request.UserId)
            {
                await _sessionManager.RevokeSessionAsync(session.Id, ct);
                _logger.LogInformation("کاربر {UserId} از سیستم خارج شد.", request.UserId);
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در خروج کاربر {UserId}", request.UserId);
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }
}