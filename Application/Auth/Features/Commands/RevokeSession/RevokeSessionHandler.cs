using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.RevokeSession;

public class RevokeSessionHandler(
    ISessionRepository sessionRepository)
    : ICommandHandler<RevokeSessionCommand>
{
    public async Task<ServiceResult> Handle(RevokeSessionCommand request, CancellationToken ct)
    {
        var sessionId = SessionId.From(request.SessionId);
        var session = await sessionRepository.GetByIdAsync(sessionId, ct);

        if (session is null)
            return ServiceResult.NotFound("جلسه یافت نشد.");

        var userId = UserId.From(request.UserId);

        if (session.UserId != userId)
            return ServiceResult.Forbidden("دسترسی غیرمجاز.");

        session.Revoke(SessionRevocationReason.UserRequested);
        sessionRepository.Update(session);

        return ServiceResult.Success();
    }
}