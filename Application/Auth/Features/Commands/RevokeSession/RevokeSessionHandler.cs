namespace Application.Auth.Features.Commands.RevokeSession;

public class RevokeSessionHandler : IRequestHandler<RevokeSessionCommand, ServiceResult>
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<RevokeSessionHandler> _logger;

    public RevokeSessionHandler(
        ISessionService sessionService,
        ILogger<RevokeSessionHandler> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessionService.GetSessionBySelectorAsync(string.Empty, cancellationToken);

            // دریافت نشست‌های فعال کاربر و بررسی مالکیت
            var activeSessions = await _sessionService.GetActiveSessionsAsync(request.UserId, cancellationToken);
            var targetSession = activeSessions.FirstOrDefault(s => s.Id == request.SessionId);

            if (targetSession == null)
                return ServiceResult.Failure("نشست یافت نشد یا متعلق به شما نیست.", 404);

            await _sessionService.RevokeSessionAsync(request.SessionId, cancellationToken);

            _logger.LogInformation(
                "نشست {SessionId} برای کاربر {UserId} ابطال شد.",
                request.SessionId, request.UserId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ابطال نشست {SessionId} برای کاربر {UserId}",
                request.SessionId, request.UserId);
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }
}