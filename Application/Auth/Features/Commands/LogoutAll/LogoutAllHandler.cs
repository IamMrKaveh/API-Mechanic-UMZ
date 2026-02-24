namespace Application.Auth.Features.Commands.LogoutAll;

public class LogoutAllHandler : IRequestHandler<LogoutAllCommand, ServiceResult>
{
    private readonly ISessionService _sessionManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<LogoutAllHandler> _logger;

    public LogoutAllHandler(
        ISessionService sessionManager,
        IAuditService auditService,
        ILogger<LogoutAllHandler> logger
        )
    {
        _sessionManager = sessionManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        LogoutAllCommand request,
        CancellationToken ct
        )
    {
        try
        {
            await _sessionManager.RevokeAllUserSessionsAsync(request.UserId, ct);

            await _auditService.LogSecurityEventAsync(
                "LogoutAll",
                $"کاربر {request.UserId} از تمام دستگاه‌ها خارج شد.",
                "system",
                request.UserId);

            _logger.LogInformation("کاربر {UserId} از تمام دستگاه‌ها خارج شد.", request.UserId);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در خروج کاربر {UserId} از تمام دستگاه‌ها", request.UserId);
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }
}