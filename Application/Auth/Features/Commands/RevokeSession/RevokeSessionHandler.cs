using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;

namespace Application.Auth.Features.Commands.RevokeSession;

public class RevokeSessionHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<RevokeSessionHandler> logger) : IRequestHandler<RevokeSessionCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        RevokeSessionCommand request,
        CancellationToken ct)
    {
        try
        {
            var user = await userRepository.GetWithSessionsAsync(request.UserId, ct);

            if (user == null)
                return ServiceResult.NotFound("کاربر یافت نشد.");

            var targetSession = user.GetActiveSessions()
                .FirstOrDefault(s => s.Id == request.SessionId);

            if (targetSession == null)
                return ServiceResult.NotFound("نشست یافت نشد.");

            user.RevokeSession(request.SessionId);

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