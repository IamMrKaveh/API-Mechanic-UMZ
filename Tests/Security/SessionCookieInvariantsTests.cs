using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Presentation.Auth.Endpoints;
using Presentation.Common.Cookies;
using Presentation.Common.Options;

namespace Tests.Security;

/// <summary>
/// Phase 6 invariants (security audit) for the refresh-token cookie and the
/// anti-forgery protection of state-changing authentication endpoints
/// (audit item 3).
/// </summary>
public class SessionCookieInvariantsTests
{
    private static AuthCookieOptions ConfiguredCookieOptions()
    {
        var settings = SecurityInvariantHelpers.ReadAppSettings();

        return new AuthCookieOptions
        {
            RefreshTokenName = SecurityInvariantHelpers.GetString(settings, "Auth", "Cookie", "RefreshTokenName")!,
            Path = SecurityInvariantHelpers.GetString(settings, "Auth", "Cookie", "Path")!,
            SameSite = SecurityInvariantHelpers.GetString(settings, "Auth", "Cookie", "SameSite")!,
            Secure = SecurityInvariantHelpers.GetBoolean(settings, "Auth", "Cookie", "Secure"),
            HttpOnly = SecurityInvariantHelpers.GetBoolean(settings, "Auth", "Cookie", "HttpOnly"),
            Domain = SecurityInvariantHelpers.GetString(settings, "Auth", "Cookie", "Domain")
        };
    }

    private static AuthCookieService CreateService() =>
        new(Options.Create(ConfiguredCookieOptions()));

    private static string WrittenCookie(HttpContext context) =>
        context.Response.Headers.SetCookie.ToString();

    [Fact]
    public void ConfiguredRefreshCookie_UsesHostPrefixAndStrictFlags()
    {
        var options = ConfiguredCookieOptions();

        options.RefreshTokenName.ShouldBe("__Host-refresh");
        options.Path.ShouldBe("/");
        options.SameSite.ShouldBe("Strict");
        options.Secure.ShouldBeTrue();
        options.HttpOnly.ShouldBeTrue();
        string.IsNullOrWhiteSpace(options.Domain).ShouldBeTrue(
            "a __Host- cookie must not declare a Domain attribute");
    }

    [Fact]
    public void WriteRefreshToken_SetsSecureHttpOnlyStrictCookie()
    {
        var context = new DefaultHttpContext();

        CreateService().WriteRefreshToken(context.Response, "refresh-token-value");

        var cookie = WrittenCookie(context).ToLowerInvariant();
        cookie.ShouldStartWith("__host-refresh=refresh-token-value");
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("secure");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain("path=/");
        cookie.ShouldNotContain("domain=");
    }

    [Fact]
    public void ClearRefreshToken_KeepsTheSecurityFlags()
    {
        var context = new DefaultHttpContext();

        CreateService().ClearRefreshToken(context.Response);

        var cookie = WrittenCookie(context).ToLowerInvariant();
        cookie.ShouldStartWith("__host-refresh=");
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("secure");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain("expires=");
        cookie.ShouldNotContain("domain=");
    }

    [Theory]
    [InlineData(nameof(AuthController.RefreshToken))]
    [InlineData(nameof(AuthController.Logout))]
    [InlineData(nameof(AuthController.LogoutAll))]
    public void AuthenticationStateChangingEndpoints_RequireAntiforgeryToken(string actionName)
    {
        var method = typeof(AuthController).GetMethod(actionName);

        method.ShouldNotBeNull();
        method!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), inherit: true)
            .ShouldNotBeEmpty($"{actionName} must keep [ValidateAntiForgeryToken] (audit item 3)");
    }
}
