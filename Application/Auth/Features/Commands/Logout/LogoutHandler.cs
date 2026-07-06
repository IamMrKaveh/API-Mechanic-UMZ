using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.Logout;

public class LogoutHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IAuditService auditService)
    : ICommandHandler<LogoutCommand>
{
    public async Task<ServiceResult> Handle(LogoutCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return ServiceResult.Success();

        if (currentUser.UserId is null)
            return ServiceResult.Success();

        var refreshToken = Domain.Security.ValueObjects.RefreshToken.Create(request.RefreshToken);
        var session = await sessionRepository.GetByRefreshTokenAsync(refreshToken, ct);

        if (session is null)
            return ServiceResult.Success();

        var userId = UserId.From(currentUser.UserId.Value);
        if (session.UserId != userId)
            return ServiceResult.Success();

        session.Revoke(SessionRevocationReason.UserRequested);
        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogSecurityEventAsync(
            "Logout",
            $"کاربر {currentUser.UserId} از سیستم خارج شد.",
            IpAddress.Unknown,
            userId,
            ct);

        return ServiceResult.Success();
    }
}