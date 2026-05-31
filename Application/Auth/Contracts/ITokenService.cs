using Domain.Security.ValueObjects;

namespace Application.Auth.Contracts;

public interface ITokenService
{
    (string? Selector, string? Verifier) ParseRefreshToken(RefreshToken refreshToken);
}