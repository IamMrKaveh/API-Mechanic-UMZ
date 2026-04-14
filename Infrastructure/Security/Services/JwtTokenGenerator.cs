using Application.Auth.Contracts;
using Infrastructure.Security.Settings;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Infrastructure.Security.Services;

public sealed class JwtTokenGenerator(IOptions<JwtSettings> jwtSettings) : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public string GenerateAccessToken(Domain.User.Aggregates.User user)
    {
        var claims = BuildClaims(user);
        return CreateToken(claims, DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes));
    }

    public (string AccessToken, string RefreshToken) GenerateTokens(Domain.User.Aggregates.User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshTokenBytes = new byte[32];
        RandomNumberGenerator.Fill(refreshTokenBytes);
        var refreshToken = Convert.ToBase64String(refreshTokenBytes);
        return (accessToken, refreshToken);
    }

    private List<System.Security.Claims.Claim> BuildClaims(Domain.User.Aggregates.User user)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.Value.ToString()),
            new(ClaimTypes.MobilePhone, user.PhoneNumber.Value),
        };

        if (user.IsAdmin)
            claims.Add(new System.Security.Claims.Claim(ClaimTypes.Role, "Admin"));

        return claims;
    }

    private string CreateToken(IEnumerable<System.Security.Claims.Claim> claims, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}