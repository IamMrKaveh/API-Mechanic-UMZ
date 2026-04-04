using Application.Common.Results;
using Domain.Security.Interfaces;

namespace Application.Auth.Features.Commands.Logout;

public class LogoutHandler : IRequestHandler<LogoutCommand, ServiceResult>
{
    private readonly ITokenService _tokenService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        ITokenService tokenService,
        ISessionRepository sessionRepository,
        IUnitOfWork unitOfWork,
        ILogger<LogoutHandler> logger)
    {
        _tokenService = tokenService;
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(
        LogoutCommand request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return ServiceResult.Success();

            var (selector, _) = _tokenService.ParseRefreshToken(request.RefreshToken);
            if (selector == null)
                return ServiceResult.Success();

            var session = await _sessionRepository.GetBySelectorAsync(selector, ct);
            if (session != null && session.UserId == request.UserId)
            {
                await _sessionRepository.RevokeAsync(session.Id, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("کاربر {UserId} از سیستم خارج شد.", request.UserId);
            }

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در خروج کاربر {UserId}", request.UserId);
            return ServiceResult.Unexpected("خطای داخلی سرور.");
        }
    }
}