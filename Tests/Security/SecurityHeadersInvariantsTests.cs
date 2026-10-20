using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Presentation.Common.Middleware;
using Presentation.Common.Options;

namespace Tests.Security;

/// <summary>
/// Phase 6 invariant (security audit, item 4): the hardened response headers that
/// are configured in appsettings must actually be emitted, including the CSP nonce
/// and the COOP/COEP/CORP isolation headers.
/// </summary>
public class SecurityHeadersInvariantsTests
{
    private static SecurityHeadersOptions ConfiguredOptions()
    {
        var settings = SecurityInvariantHelpers.ReadAppSettings();

        return new SecurityHeadersOptions
        {
            DisableCsp = SecurityInvariantHelpers.GetBoolean(settings, "SecurityHeaders", "DisableCsp"),
            EnableNonce = SecurityInvariantHelpers.GetBoolean(settings, "SecurityHeaders", "EnableNonce"),
            EnableCoop = SecurityInvariantHelpers.GetBoolean(settings, "SecurityHeaders", "EnableCoop"),
            EnableCoep = SecurityInvariantHelpers.GetBoolean(settings, "SecurityHeaders", "EnableCoep"),
            EnableCorp = SecurityInvariantHelpers.GetBoolean(settings, "SecurityHeaders", "EnableCorp"),
            HstsMaxAge = SecurityInvariantHelpers.GetInt32(settings, "SecurityHeaders", "HstsMaxAge"),
            HstsPreload = SecurityInvariantHelpers.GetBoolean(settings, "SecurityHeaders", "HstsPreload")
        };
    }

    private static SecurityHeadersMiddleware CreateMiddleware() =>
        new(_ => Task.CompletedTask, Options.Create(ConfiguredOptions()));

    private static async Task<HttpContext> InvokeAsync()
    {
        var context = new DefaultHttpContext();
        await CreateMiddleware().InvokeAsync(context);
        return context;
    }

    [Fact]
    public async Task ConfiguredHeaders_AreEmittedTogether()
    {
        var context = await InvokeAsync();
        var headers = context.Response.Headers;

        headers["Cross-Origin-Opener-Policy"].ToString().ShouldBe("same-origin");
        headers["Cross-Origin-Embedder-Policy"].ToString().ShouldBe("require-corp");
        headers["Cross-Origin-Resource-Policy"].ToString().ShouldBe("same-origin");
        headers["X-Frame-Options"].ToString().ShouldBe("SAMEORIGIN");
        headers["X-Content-Type-Options"].ToString().ShouldBe("nosniff");
        headers["Strict-Transport-Security"].ToString().ShouldContain("max-age=");
        headers["Content-Security-Policy"].ToString().ShouldContain("default-src");
    }

    [Fact]
    public async Task ConfiguredHeaders_IncludeACspNonce()
    {
        var context = await InvokeAsync();

        var nonce = context.Items["CspNonce"].ShouldBeOfType<string>();
        nonce.ShouldNotBeNullOrWhiteSpace();
        Convert.FromBase64String(nonce).Length.ShouldBe(16);

        context.Response.Headers["Content-Security-Policy"].ToString()
            .ShouldContain($"'nonce-{nonce}'");
    }

    [Fact]
    public async Task CspNonce_DiffersBetweenRequests()
    {
        var first = (string)(await InvokeAsync()).Items["CspNonce"]!;
        var second = (string)(await InvokeAsync()).Items["CspNonce"]!;

        first.ShouldNotBe(second);
    }

    [Fact]
    public async Task Csp_DoesNotAllowUnsafeEval()
    {
        var context = await InvokeAsync();

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();

        csp.ShouldNotContain("unsafe-eval");
        csp.ShouldContain("object-src 'none'");
        csp.ShouldContain("frame-ancestors 'none'");
    }
}
