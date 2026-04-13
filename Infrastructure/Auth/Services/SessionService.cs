using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Domain.Common.Interfaces;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Auth.Services;

public class SessionService(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork) : ISessionService
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public Task<ServiceResult<RefreshTokenResult>> CreateSessionAsync(UserId userId, IpAddress ipAddress, string? userAgent, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ServiceResult<RefreshTokenResult>> RefreshSessionAsync(RefreshToken refreshToken, IpAddress ipAddress, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task RevokeAllSessionsAsync(UserId userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task RevokeAllUserSessionsAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        await _sessionRepository.RevokeAllByUserAsync(userId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task RevokeSessionAsync(SessionId sessionId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}