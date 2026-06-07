using Application.Auth.Contracts;
using Infrastructure.Security.Options;
using SharedKernel.Constants;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Infrastructure.Auth.Services;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> jwtOptions) : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public string GenerateAccessToken(Domain.User.Aggregates.User user)
    {
        var claims = BuildClaims(user);
        return CreateToken(claims, DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes));
    }

    public (string AccessToken, string RefreshToken) GenerateTokens(Domain.User.Aggregates.User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshTokenBytes = new byte[32];
        RandomNumberGenerator.Fill(refreshTokenBytes);
        var refreshToken = Convert.ToBase64String(refreshTokenBytes);
        return (accessToken, refreshToken);
    }

    private static List<Claim> BuildClaims(Domain.User.Aggregates.User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.Value.ToString()),
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