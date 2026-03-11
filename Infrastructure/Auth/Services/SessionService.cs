using Domain.Security.Interfaces;

namespace Infrastructure.Auth.Services;

public class SessionService(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork) : ISessionService
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task RevokeAllUserSessionsAsync(
        int userId,
        CancellationToken ct = default)
    {
        await _sessionRepository.RevokeAllByUserAsync(userId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}