using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Auth.Features.Commands.RevokeSession;

public class RevokeSessionHandler(
    ISessionRepository sessionRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RevokeSessionCommand>
{
    public async Task<ServiceResult> Handle(RevokeSessionCommand request, CancellationToken ct)
    {
        var sessionId = SessionId.From(request.SessionId);
        var sessionResult = await (sessionRepository.GetByIdAsync(sessionId, ct)).OrNotFoundAsync("جلسه یافت نشد.");
        if (sessionResult.IsFailure) return sessionResult.Error;
        var session = sessionResult.Value;

        var userId = UserId.From(currentUserService.UserId!.Value);

        if (session.UserId != userId)
            return ServiceResult.Forbidden("دسترسی غیرمجاز.");

        var now = dateTimeProvider.UtcNow;
        session.Revoke(now, SessionRevocationReason.UserRequested);
        sessionRepository.Update(session);

        return ServiceResult.Success();
    }
}
