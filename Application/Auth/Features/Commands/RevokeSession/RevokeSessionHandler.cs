using Domain.Security.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Commands.RevokeSession;

public class RevokeSessionHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<RevokeSessionHandler> logger) : IRequestHandler<RevokeSessionCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        RevokeSessionCommand request,
        CancellationToken ct)
    {
        try
        {
            var userId = UserId.From(request.UserId);
            var user = await sessionRepository.GetActiveSessionCountAsync(userId, ct);

            if (user == 0)
                return ServiceResult.NotFound("کاربر یافت نشد.");

            var targetSession = sessionRepository.GetActiveByUserIdAsync(userId, ct);

            if (targetSession is null)
                return ServiceResult.NotFound("نشست یافت نشد.");

            sessionRepository.RevokeAsync(targetSession.Result.FirstOrDefault(x => x.UserId == userId).Id);

            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "نشست {SessionId} برای کاربر {UserId} ابطال شد.",
                request.SessionId, request.UserId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در ابطال نشست {SessionId} برای کاربر {UserId}",
                request.SessionId, request.UserId);
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }
}