using Domain.Security.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.LogoutAll;

public class LogoutAllHandler(
    ISessionRepository sessionRepository,
    ICurrentUserService currentUserService)
    : ICommandHandler<LogoutAllCommand>
{
    public async Task<ServiceResult> Handle(
        LogoutAllCommand request,
        CancellationToken ct)
    {
        var effectiveId = request.TargetUserId ?? currentUserService.UserId
            ?? throw new InvalidOperationException("User context not resolved.");

        var userId = UserId.From(effectiveId);
        await sessionRepository.RevokeAllByUserIdAsync(userId, ct);
        return ServiceResult.Success();
    }
}