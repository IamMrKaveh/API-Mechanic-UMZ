using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Auth.Features.Commands.AdminRevokeSession;

public class AdminRevokeSessionHandler(
    ISessionRepository sessionRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AdminRevokeSessionCommand>
{
    public async Task<ServiceResult> Handle(AdminRevokeSessionCommand request, CancellationToken ct)
    {
        var sessionId = SessionId.From(request.SessionId);
        var session = await sessionRepository.GetByIdAsync(sessionId, ct);

        if (session is null)
            return ServiceResult.NotFound("جلسه یافت نشد.");

        var targetUserId = UserId.From(request.TargetUserId);

        if (session.UserId != targetUserId)
            return ServiceResult.NotFound("جلسه متعلق به این کاربر نیست.");

        var now = dateTimeProvider.UtcNow;
        session.Revoke(now, SessionRevocationReason.AdminRevoked);
        sessionRepository.Update(session);

        return ServiceResult.Success();
    }
}