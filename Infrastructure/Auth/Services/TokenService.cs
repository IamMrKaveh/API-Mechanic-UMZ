using Application.Auth.Contracts;
using Domain.Security.ValueObjects;

namespace Infrastructure.Auth.Services;

public sealed class TokenService() : ITokenService
{
    public (string? Selector, string? Verifier) ParseRefreshToken(RefreshToken refreshToken)
    {
        var parts = refreshToken.Value.Split('.');
        if (parts.Length != 2)
            return (null, null);

        return (parts[0], parts[1]);
    }
}