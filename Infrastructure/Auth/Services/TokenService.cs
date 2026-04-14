using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Infrastructure.Auth.Options;
using Infrastructure.Persistence.Context;
using Infrastructure.Security.Options;

namespace Infrastructure.Auth.Services;

public sealed class TokenService(
    ISessionRepository sessionRepository,
    IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public (string? Selector, string? Verifier) ParseRefreshToken(RefreshToken refreshToken)
    {
        var parts = refreshToken.Value.Split('.');
        if (parts.Length != 2)
            return (null, null);

        return (parts[0], parts[1]);
    }

    public async Task<RefreshTokenResult?> GetByTokenAsync(
        RefreshToken refreshToken,
        CancellationToken ct = default)
    {
        var (selector, _) = ParseRefreshToken(refreshToken);
        if (selector is null)
            return null;

        var session = await sessionRepository.GetBySelectorAsync(selector, ct);
        if (session is null || session.IsRevoked || session.ExpiresAt < DateTime.UtcNow)
            return null;

        return new RefreshTokenResult(
            session.Id.Value,
            refreshToken.Value,
            session.ExpiresAt,
            session.UserId.Value);
    }
}