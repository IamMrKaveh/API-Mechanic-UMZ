namespace Infrastructure.Auth.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _accessTokenExpirationMinutes = configuration.GetValue("Jwt:AccessTokenExpirationMinutes", 60);
        _refreshTokenExpirationDays = configuration.GetValue("Jwt:RefreshTokenExpirationDays", 7);
    }

    /// <summary>
    /// تولید JWT Token
    /// </summary>
    public string GenerateJwtToken(Domain.User.User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "کاربر نمی‌تواند خالی باشد.");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.MobilePhone, user.PhoneNumber),
        };

        if (!string.IsNullOrEmpty(user.FirstName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
        }

        if (!string.IsNullOrEmpty(user.LastName))
        {
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
        }

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        claims.Add(new Claim(ClaimTypes.Role, "User"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Audience = _configuration["Jwt:Audience"],
            Issuer = _configuration["Jwt:Issuer"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// تولید Refresh Token امن
    /// </summary>
    public RefreshTokenResult GenerateRefreshToken()
    {
        
        var selectorBytes = new byte[32];
        RandomNumberGenerator.Fill(selectorBytes);
        var selector = Convert.ToBase64String(selectorBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        
        var verifierBytes = new byte[32];
        RandomNumberGenerator.Fill(verifierBytes);
        var verifier = Convert.ToBase64String(verifierBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        
        var fullToken = $"{selector}.{verifier}";

        return new RefreshTokenResult
        {
            Selector = selector,
            Verifier = verifier,
            FullToken = fullToken
        };
    }

    /// <summary>
    /// هش کردن Refresh Token
    /// </summary>
    public string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentNullException(nameof(token), "توکن نمی‌تواند خالی باشد.");

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// استخراج اطلاعات از JWT منقضی شده (بدون اعتبارسنجی امضا)
    /// </summary>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
                ValidateLifetime = false 
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// تجزیه Refresh Token به Selector و Verifier
    /// </summary>
    public (string? Selector, string? Verifier) ParseRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return (null, null);

        var parts = refreshToken.Split('.');
        if (parts.Length != 2)
            return (null, null);

        return (parts[0], parts[1]);
    }

    /// <summary>
    /// دریافت زمان انقضای Access Token
    /// </summary>
    public DateTime GetAccessTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);
    }

    /// <summary>
    /// دریافت زمان انقضای Refresh Token
    /// </summary>
    public DateTime GetRefreshTokenExpiration()
    {
        return DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);
    }
}