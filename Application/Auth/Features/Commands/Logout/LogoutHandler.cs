using Domain.Common.ValueObjects;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.Logout;

public class LogoutHandler(
    ITokenService tokenService,
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<LogoutCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(LogoutCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return ServiceResult.Success();

        var refreshToken = Domain.Security.ValueObjects.RefreshToken.Create(request.RefreshToken);
        var (selector, _) = tokenService.ParseRefreshToken(refreshToken);

        if (selector is null)
            return ServiceResult.Success();

        var session = await sessionRepository.GetByRefreshTokenAsync(refreshToken, ct);

        if (session is not null && session.UserId == UserId.From(request.UserId))
        {
            session.Revoke(SessionRevocationReason.UserRequested);
            sessionRepository.Update(session);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogSecurityEventAsync(
                "Logout",
                $"کاربر {request.UserId} از سیستم خارج شد.",
                IpAddress.Unknown,
                UserId.From(request.UserId));
        }

        return ServiceResult.Success();
    }
}