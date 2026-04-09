using Application.Audit.Contracts;
using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Security.Interfaces;

namespace Application.Auth.Features.Commands.LogoutAll;

public class LogoutAllHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ILogger<LogoutAllHandler> logger) : IRequestHandler<LogoutAllCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        LogoutAllCommand request,
        CancellationToken ct)
    {
        try
        {
            await sessionRepository.RevokeAllByUserAsync(request.UserId, ct);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSecurityEventAsync(
                "LogoutAll",
                $"کاربر {request.UserId} از تمام دستگاه‌ها خارج شد.",
                "system",
                request.UserId);

            logger.LogInformation("کاربر {UserId} از تمام دستگاه‌ها خارج شد.", request.UserId);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در خروج کاربر {UserId} از تمام دستگاه‌ها", request.UserId);
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }
}