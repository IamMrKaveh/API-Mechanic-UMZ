using Domain.Common.ValueObjects;
using Domain.Security.Interfaces;
using Domain.User.ValueObjects;

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
            var userId = UserId.From(request.UserId);
            var ipAddress = IpAddress.Unknown;

            await sessionRepository.RevokeAllByUserAsync(userId, ct);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSecurityEventAsync(
                "LogoutAll",
                $"کاربر {request.UserId} از تمام دستگاه‌ها خارج شد.",
                ipAddress,
                userId);

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