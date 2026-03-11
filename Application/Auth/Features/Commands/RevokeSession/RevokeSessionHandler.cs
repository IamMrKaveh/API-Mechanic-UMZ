using Application.Common.Models;
using Domain.User.Interfaces;

namespace Application.Auth.Features.Commands.RevokeSession;

public class RevokeSessionHandler : IRequestHandler<RevokeSessionCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeSessionHandler> _logger;

    public RevokeSessionHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<RevokeSessionHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        RevokeSessionCommand request,
        CancellationToken ct)
    {
        try
        {
            var user = await _userRepository.GetWithSessionsAsync(request.UserId, ct);

            if (user == null)
                return ServiceResult.Failure("کاربر یافت نشد.", 404);

            var targetSession = user.GetActiveSessions()
                .FirstOrDefault(s => s.Id == request.SessionId);

            if (targetSession == null)
                return ServiceResult.Failure("نشست یافت نشد یا متعلق به شما نیست.", 404);

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
            return ServiceResult.Failure("خطای داخلی سرور.");
        }
    }
}