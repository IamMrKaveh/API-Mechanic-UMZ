using Domain.Common.ValueObjects;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.RevokeSession;

public class RevokeSessionHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<RevokeSessionCommand, ServiceResult>
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

        session.Revoke(SessionRevocationReason.AdminRevoked);
        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogSecurityEventAsync(
            "RevokeSession",
            $"جلسه {request.SessionId} توسط کاربر {request.UserId} لغو شد.",
            IpAddress.Unknown,
            userId);

        return ServiceResult.Success();
    }
}