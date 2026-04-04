using Application.Common.Results;
using Domain.Security.Interfaces;

namespace Application.Auth.Features.Commands.LogoutAll;

public class LogoutAllHandler : IRequestHandler<LogoutAllCommand, ServiceResult>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<LogoutAllHandler> _logger;

    public LogoutAllHandler(
        ISessionRepository sessionRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<LogoutAllHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        LogoutAllCommand request,
        CancellationToken ct)
    {
        try
        {
            await _sessionRepository.RevokeAllByUserAsync(request.UserId, ct);
            await _unitOfWork.SaveChangesAsync(ct);

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
            return ServiceResult.Unexpected("خطای داخلی سرور.");
        }
    }
}