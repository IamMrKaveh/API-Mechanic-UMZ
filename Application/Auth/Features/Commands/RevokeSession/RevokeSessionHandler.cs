using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;

namespace Application.Auth.Features.Commands.RevokeSession;

public class RevokeSessionHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<RevokeSessionHandler> logger) : IRequestHandler<RevokeSessionCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<RevokeSessionHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        RevokeSessionCommand request,
        CancellationToken ct)
    {
        try
        {
            var user = await _userRepository.GetWithSessionsAsync(request.UserId, ct);

            if (user == null)
                return ServiceResult.NotFound("کاربر یافت نشد.");

            var targetSession = user.GetActiveSessions()
                .FirstOrDefault(s => s.Id == request.SessionId);

            if (targetSession == null)
                return ServiceResult.NotFound("نشست یافت نشد.");

            user.RevokeSession(request.SessionId);

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "نشست {SessionId} برای کاربر {UserId} ابطال شد.",
                request.SessionId, request.UserId);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ابطال نشست {SessionId} برای کاربر {UserId}",
                request.SessionId, request.UserId);
            return ServiceResult.Unexpected("خطای داخلی سرور.");
        }
    }
}