using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class AntiforgeryExtensionsTests
{
    [Fact]
    public void AddApplicationAntiforgery_ConfiguresExpectedOptions()
    {
        var services = new ServiceCollection();
        services.AddApplicationAntiforgery();

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<AntiforgeryOptions>>().Value;

        options.HeaderName.ShouldBe("X-XSRF-TOKEN");
        options.FormFieldName.ShouldBe("__RequestVerificationToken");
        options.Cookie.Name.ShouldBe("XSRF-TOKEN");
        options.Cookie.HttpOnly.ShouldBeFalse();
        options.Cookie.SecurePolicy.ShouldBe(CookieSecurePolicy.Always);
        options.Cookie.SameSite.ShouldBe(SameSiteMode.Strict);
        options.SuppressXFrameOptionsHeader.ShouldBeFalse();
    }

    [Fact]
    public void AddApplicationAntiforgery_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddApplicationAntiforgery().ShouldBeSameAs(services);
    }

    private static (ApplicationBuilder App, IAntiforgery Antiforgery) BuildApp(
        AntiforgeryTokenSet tokens)
    {
        var antiforgery = Substitute.For<IAntiforgery>();
        antiforgery.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(tokens);
        var services = new ServiceCollection();
        services.AddSingleton(antiforgery);
        var app = new ApplicationBuilder(services.BuildServiceProvider());
        return (app, antiforgery);
    }

    private static DefaultHttpContext BuildContext(
        IServiceProvider services,
        bool authenticated,
        string method = "GET",
        string path = "/api/orders")
    {
        var context = new DefaultHttpContext();
        context.RequestServices = services;
        context.Request.Method = method;
        context.Request.Path = path;
        context.User = authenticated
            ? new ClaimsPrincipal(new ClaimsIdentity("mock"))
            : new ClaimsPrincipal();
        return context;
    }

    [Fact]
    public async Task UseApplicationAntiforgery_AuthenticatedGet_SetsXsrfCookie()
    {
        var (app, antiforgery) = BuildApp(new AntiforgeryTokenSet("req-token", "cookie", "field", "header"));
        app.UseApplicationAntiforgery();
        var called = false;
        app.Run(_ => { called = true; return Task.CompletedTask; });
        var pipeline = app.Build();
        var context = BuildContext(app.ApplicationServices, authenticated: true);

        await pipeline(context);

        called.ShouldBeTrue();
        antiforgery.Received(1).GetAndStoreTokens(Arg.Any<HttpContext>());
        context.Response.Headers.SetCookie.ToString().ShouldContain("XSRF-TOKEN=req-token");
    }

    [Fact]
    public async Task UseApplicationAntiforgery_UnauthenticatedRequest_SkipsToken()
    {
        var (app, antiforgery) = BuildApp(new AntiforgeryTokenSet("req-token", "cookie", "field", "header"));
        app.UseApplicationAntiforgery();
        app.Run(_ => Task.CompletedTask);
        var context = BuildContext(app.ApplicationServices, authenticated: false);

        await app.Build()(context);

        antiforgery.DidNotReceiveWithAnyArgs().GetAndStoreTokens(Arg.Any<HttpContext>());
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/metrics")]
    [InlineData("/swagger/index.html")]
    [InlineData("/HEALTH/ready")]
    public async Task UseApplicationAntiforgery_ExcludedPaths_SkipToken(string path)
    {
        var (app, antiforgery) = BuildApp(new AntiforgeryTokenSet("req-token", "cookie", "field", "header"));
        app.UseApplicationAntiforgery();
        app.Run(_ => Task.CompletedTask);
        var context = BuildContext(app.ApplicationServices, authenticated: true, path: path);

        await app.Build()(context);

        antiforgery.DidNotReceiveWithAnyArgs().GetAndStoreTokens(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task UseApplicationAntiforgery_NonGetRequest_SkipsToken()
    {
        var (app, antiforgery) = BuildApp(new AntiforgeryTokenSet("req-token", "cookie", "field", "header"));
        app.UseApplicationAntiforgery();
        app.Run(_ => Task.CompletedTask);
        var context = BuildContext(app.ApplicationServices, authenticated: true, method: "POST");

        await app.Build()(context);

        antiforgery.DidNotReceiveWithAnyArgs().GetAndStoreTokens(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task UseApplicationAntiforgery_EmptyRequestToken_SetsNoCookie()
    {
        var (app, _) = BuildApp(new AntiforgeryTokenSet(null, "cookie", "field", "header"));
        app.UseApplicationAntiforgery();
        var called = false;
        app.Run(_ => { called = true; return Task.CompletedTask; });
        var context = BuildContext(app.ApplicationServices, authenticated: true);

        await app.Build()(context);

        called.ShouldBeTrue();
        context.Response.Headers.ContainsKey("Set-Cookie").ShouldBeFalse();
    }

    [Fact]
    public void UseApplicationAntiforgery_ReturnsSameBuilder()
    {
        var (app, _) = BuildApp(new AntiforgeryTokenSet("t", "c", "f", "h"));

        app.UseApplicationAntiforgery().ShouldBeSameAs(app);
    }
}
