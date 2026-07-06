using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Domain.Security.ValueObjects;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Constants;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Auth.Services;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> jwtOptions) : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public string GenerateAccessToken(Domain.User.Aggregates.User user, SessionId sessionId)
    {
        var claims = BuildClaims(user, sessionId);
        return CreateToken(claims, DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes));
    }

    private static List<Claim> BuildClaims(Domain.User.Aggregates.User user, SessionId sessionId)
    {
        var userId = user.Id.Value.ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sid, sessionId.Value.ToString()),
            new("sid", sessionId.Value.ToString()),
            new("nameid", userId),
            new(ClaimTypes.MobilePhone, user.PhoneNumber.Value),
            new(ClaimTypes.Role, AppRoles.User),
        };

        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, AppRoles.Admin));

        return claims;
    }

    private string CreateToken(IEnumerable<Claim> claims, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}