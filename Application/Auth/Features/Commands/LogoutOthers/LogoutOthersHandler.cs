using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.LogoutOthers;

public class LogoutOthersHandler(
    ISessionRepository sessionRepository,
    ICurrentUserService currentUserService)
    : ICommandHandler<LogoutOthersCommand>
{
    public async Task<ServiceResult> Handle(LogoutOthersCommand request, CancellationToken ct)
    {
        if (currentUserService.UserId is null)
            return ServiceResult.Unauthorized("کاربر احراز هویت نشده است.");

        var userId = UserId.From(currentUserService.UserId.Value);

        if (currentUserService.SessionId is null || currentUserService.SessionId == Guid.Empty)
        {
            await sessionRepository.RevokeAllByUserIdAsync(userId, SessionRevocationReason.UserRequested, ct);
            return ServiceResult.Success();
        }

        var currentSessionId = SessionId.From(currentUserService.SessionId.Value);
        await sessionRepository.RevokeAllExceptAsync(userId, currentSessionId, SessionRevocationReason.UserRequested, ct);

        return ServiceResult.Success();
    }
}
