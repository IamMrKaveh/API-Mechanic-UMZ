using Domain.Security.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.LogoutAll;

public class LogoutAllHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<LogoutAllCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        LogoutAllCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var ipAddress = IpAddress.Unknown;

        await sessionRepository.RevokeAllByUserIdAsync(userId, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogSecurityEventAsync(
            "LogoutAll",
            $"کاربر {request.UserId} از تمام دستگاه‌ها خارج شد.",
            ipAddress,
            userId);

        return ServiceResult.Success();
    }
}