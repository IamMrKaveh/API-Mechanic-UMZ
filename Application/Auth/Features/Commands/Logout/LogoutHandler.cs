using Domain.Security.Interfaces;

namespace Application.Auth.Features.Commands.Logout;

public class LogoutHandler(
    ITokenService tokenService,
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ILogger<LogoutHandler> logger) : IRequestHandler<LogoutCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        LogoutCommand request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return ServiceResult.Success();

            var refreshToken = Domain.Security.ValueObjects.RefreshToken.Create(request.RefreshToken);

            var (selector, _) = tokenService.ParseRefreshToken(refreshToken);
            if (selector is null)
                return ServiceResult.Success();

            var session = await sessionRepository.GetBySelectorAsync(selector, ct);

            if (session is not null && session.UserId == request.UserId)
            {
                await sessionRepository.RevokeAsync(session.Id, ct);
                await unitOfWork.SaveChangesAsync(ct);
                logger.LogInformation("کاربر {UserId} از سیستم خارج شد.", request.UserId);
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در خروج کاربر {UserId}", request.UserId);
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }
}