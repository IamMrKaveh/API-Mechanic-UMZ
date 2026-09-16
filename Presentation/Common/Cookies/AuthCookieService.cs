using Microsoft.Extensions.Options;
using Presentation.Common.Options;

namespace Presentation.Common.Cookies;

public sealed class AuthCookieService(IOptions<AuthCookieOptions> options) : IAuthCookieService
{
    private static readonly DateTimeOffset ClearExpires = DateTimeOffset.UnixEpoch;

    private AuthCookieOptions Options => options.Value;

    public string? ReadRefreshToken(HttpRequest request)
    {
        return request.Cookies.TryGetValue(Options.RefreshTokenName, out var token)
            ? token
            : null;
    }

    public void WriteRefreshToken(HttpResponse response, string refreshToken, DateTime expiresAtUtc)
    {
        response.Cookies.Append(
            Options.RefreshTokenName,
            refreshToken,
            BuildOptions(new DateTimeOffset(expiresAtUtc, TimeSpan.Zero)));
    }

    public void WriteRefreshToken(HttpResponse response, string refreshToken)
    {
        response.Cookies.Append(Options.RefreshTokenName, refreshToken, BuildOptions(null));
    }

    public void ClearRefreshToken(HttpResponse response)
    {
        response.Cookies.Delete(Options.RefreshTokenName, BuildOptions(ClearExpires));
    }

    private CookieOptions BuildOptions(DateTimeOffset? expires)
    {
        var sameSite = Enum.TryParse<SameSiteMode>(
            Options.SameSite,
            ignoreCase: true,
            out var parsed)
            ? parsed
            : SameSiteMode.Strict;

        return new CookieOptions
        {
            Path = Options.Path,
            Secure = Options.Secure,
            HttpOnly = Options.HttpOnly,
            IsEssential = true,
            Domain = string.IsNullOrWhiteSpace(Options.Domain) ? null : Options.Domain,
            SameSite = sameSite,
            Expires = expires
        };
    }
}
