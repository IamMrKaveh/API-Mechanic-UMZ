using Application.Auth.Features.Shared;

namespace Application.Auth.Contracts;

public interface ITokenService
{
    (string? Selector, string? Verifier) ParseRefreshToken(string refreshToken);

    Task<RefreshTokenResult?> GetByTokenAsync(string refreshToken, CancellationToken ct = default);
}