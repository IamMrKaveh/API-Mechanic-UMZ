using Application.Auth.Features.Shared;
using Domain.Security.ValueObjects;

namespace Application.Auth.Contracts;

public interface ITokenService
{
    (string? Selector, string? Verifier) ParseRefreshToken(RefreshToken refreshToken);

    Task<RefreshTokenResult?> GetByTokenAsync(
        RefreshToken refreshToken,
        CancellationToken ct = default);
}