using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Presentation.Common.Middleware;
using Presentation.Common.Options;

namespace Tests.Presentation.Common.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private static DefaultHttpContext BuildContext() => new();

    private static SecurityHeadersMiddleware BuildSut(
        SecurityHeadersOptions? options = null) =>
        new(_ => Task.CompletedTask, Options.Create(options ?? new SecurityHeadersOptions()));

    [Fact]
    public async Task InvokeAsync_DefaultOptions_AppliesBaselineHeaders()
    {
        var context = BuildContext();

        await BuildSut().InvokeAsync(context);

        context.Response.Headers["X-Content-Type-Options"].ToString().ShouldBe("nosniff");
        context.Response.Headers["X-XSS-Protection"].ToString().ShouldBe("1; mode=block");
        context.Response.Headers["X-Frame-Options"].ToString().ShouldBe("SAMEORIGIN");
        context.Response.Headers["Referrer-Policy"].ToString().ShouldBe("strict-origin-when-cross-origin");
        context.Response.Headers["Strict-Transport-Security"].ToString()
            .ShouldBe("max-age=31536000; includeSubDomains; preload");
    }

    [Fact]
    public async Task InvokeAsync_DefaultOptions_AppliesDefaultCsp()
    {
        var context = BuildContext();

        await BuildSut().InvokeAsync(context);

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.ShouldContain("default-src 'self'");
        csp.ShouldContain("base-uri 'self'");
        csp.ShouldContain("form-action 'self'");
        csp.ShouldContain("object-src 'none'");
        csp.ShouldNotContain("nonce-");
    }

    [Fact]
    public async Task InvokeAsync_WhenNonceEnabled_StoresNonceAndAddsToScriptSrc()
    {
        var context = BuildContext();

        await BuildSut(new SecurityHeadersOptions { EnableNonce = true }).InvokeAsync(context);

        var nonce = context.Items["CspNonce"].ShouldBeOfType<string>();
        nonce.ShouldNotBeNullOrWhiteSpace();
        context.Response.Headers["Content-Security-Policy"].ToString()
            .ShouldContain($"'nonce-{nonce}'");
    }

    [Fact]
    public async Task InvokeAsync_WhenNonceDisabled_NoNonceStored()
    {
        var context = BuildContext();

        await BuildSut(new SecurityHeadersOptions { EnableNonce = false }).InvokeAsync(context);

        context.Items.ContainsKey("CspNonce").ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenCspDisabled_NoCspHeader()
    {
        var context = BuildContext();

        await BuildSut(new SecurityHeadersOptions { DisableCsp = true }).InvokeAsync(context);

        context.Response.Headers.ContainsKey("Content-Security-Policy").ShouldBeFalse();
        context.Response.Headers["X-Content-Type-Options"].ToString().ShouldBe("nosniff");
    }

    [Fact]
    public async Task InvokeAsync_WhenHstsMaxAgeIsZero_NoHstsHeader()
    {
        var context = BuildContext();

        await BuildSut(new SecurityHeadersOptions { HstsMaxAge = 0 }).InvokeAsync(context);

        context.Response.Headers.ContainsKey("Strict-Transport-Security").ShouldBeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenHstsPreloadFalse_OmitsPreload()
    {
        var context = BuildContext();

        await BuildSut(new SecurityHeadersOptions { HstsMaxAge = 100, HstsPreload = false })
            .InvokeAsync(context);

        context.Response.Headers["Strict-Transport-Security"].ToString()
            .ShouldBe("max-age=100; includeSubDomains");
    }

    [Fact]
    public async Task InvokeAsync_WhenCoopCoepCorpEnabled_AppliesCrossOriginHeaders()
    {
        var context = BuildContext();

        await BuildSut(new SecurityHeadersOptions
        {
            EnableCoop = true,
            EnableCoep = true,
            EnableCorp = true
        }).InvokeAsync(context);

        context.Response.Headers["Cross-Origin-Opener-Policy"].ToString().ShouldBe("same-origin");
        context.Response.Headers["Cross-Origin-Embedder-Policy"].ToString().ShouldBe("require-corp");
        context.Response.Headers["Cross-Origin-Resource-Policy"].ToString().ShouldBe("same-origin");
    }

    [Fact]
    public async Task InvokeAsync_WhenOptionalHeadersEmpty_OmitsThem()
    {
        var context = BuildContext();

        await BuildSut(new SecurityHeadersOptions
        {
            XFrameOptions = null,
            ReferrerPolicy = "",
            PermissionsPolicy = null,
            CspDefaultSrc = null,
            CspScriptSrc = null,
            CspStyleSrc = null,
            CspImgSrc = null,
            CspFontSrc = null,
            CspConnectSrc = null,
            CspFrameAncestors = null
        }).InvokeAsync(context);

        context.Response.Headers.ContainsKey("X-Frame-Options").ShouldBeFalse();
        context.Response.Headers.ContainsKey("Referrer-Policy").ShouldBeFalse();
        context.Response.Headers.ContainsKey("Permissions-Policy").ShouldBeFalse();
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.ShouldNotContain("default-src");
        csp.ShouldContain("base-uri 'self'");
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext()
    {
        var called = false;
        var sut = new SecurityHeadersMiddleware(
            _ => { called = true; return Task.CompletedTask; },
            Options.Create(new SecurityHeadersOptions()));
        var context = BuildContext();

        await sut.InvokeAsync(context);

        called.ShouldBeTrue();
    }
}
