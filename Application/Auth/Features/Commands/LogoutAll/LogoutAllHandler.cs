using Domain.Security.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.LogoutAll;

public class LogoutAllHandler(
    ISessionRepository sessionRepository)
    : ICommandHandler<LogoutAllCommand>
{
    public async Task<ServiceResult> Handle(
        LogoutAllCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        await sessionRepository.RevokeAllByUserIdAsync(userId, ct);

        return ServiceResult.Success();
    }
}