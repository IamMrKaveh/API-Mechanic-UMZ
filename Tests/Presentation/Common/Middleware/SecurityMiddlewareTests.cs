using Infrastructure.Security.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Presentation.Common.Middleware;

namespace Tests.Presentation.Common.Middleware;

public class SecurityMiddlewareTests
{
    private readonly ILogger<SecurityMiddleware> _logger =
        Substitute.For<ILogger<SecurityMiddleware>>();

    private SecurityMiddleware BuildSut(RequestDelegate next, List<string>? whitelist = null) =>
        new(next,
            Options.Create(new SecuritySettings { AdminIpWhitelist = whitelist ?? [] }),
            _logger);

    private static DefaultHttpContext BuildContext(string path, string? ip)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.Connection.RemoteIpAddress = ip is null ? null : IPAddress.Parse(ip);
        return context;
    }

    [Fact]
    public async Task InvokeAsync_NonAdminPath_CallsNextWithoutChecks()
    {
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; }, ["9.9.9.9"]);

        await sut.InvokeAsync(BuildContext("/api/orders", "1.1.1.1"));

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_AdminPathWithEmptyWhitelist_CallsNext()
    {
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; }, []);

        await sut.InvokeAsync(BuildContext("/api/admin/users", "1.1.1.1"));

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_AdminPathWithWhitelistedIp_CallsNext()
    {
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; }, ["1.1.1.1"]);

        await sut.InvokeAsync(BuildContext("/api/admin/users", "1.1.1.1"));

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_AdminPathWithLoopbackIp_CallsNextEvenWhenNotListed()
    {
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; }, ["9.9.9.9"]);

        await sut.InvokeAsync(BuildContext("/api/admin/users", "127.0.0.1"));

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_AdminPathWithForeignIp_Returns403()
    {
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; }, ["9.9.9.9"]);
        var context = BuildContext("/api/admin/users", "1.1.1.1");

        await sut.InvokeAsync(context);

        called.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        new StreamReader(context.Response.Body).ReadToEnd().ShouldBe("Access denied.");
    }

    [Fact]
    public async Task InvokeAsync_AdminPathWithNullIp_Returns403()
    {
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; }, ["9.9.9.9"]);
        var context = BuildContext("/api/admin/users", null);

        await sut.InvokeAsync(context);

        called.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void UseAdminIpWhitelist_ReturnsBuilder()
    {
        var app = new ApplicationBuilder(
            new ServiceCollection().BuildServiceProvider());

        app.UseAdminIpWhitelist().ShouldBeSameAs(app);
    }
}
