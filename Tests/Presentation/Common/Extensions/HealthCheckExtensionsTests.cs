using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class HealthCheckExtensionsTests
{
    private static WebApplication BuildAppWithHealthChecks()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks();
        builder.Services.AddAuthorization();
        var app = builder.Build();
        app.MapApplicationHealthChecks();
        return app;
    }

    private static List<RouteEndpoint> RouteEndpoints(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(d => d.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

    [Fact]
    public void MapApplicationHealthChecks_MapsAllThreeEndpoints()
    {
        using var app = BuildAppWithHealthChecks();

        var routes = RouteEndpoints(app).Select(e => e.RoutePattern.RawText).ToList();

        routes.ShouldContain(HealthCheckExtensions.LiveEndpoint);
        routes.ShouldContain(HealthCheckExtensions.ReadyEndpoint);
        routes.ShouldContain(HealthCheckExtensions.DetailsEndpoint);
    }

    [Fact]
    public void MapApplicationHealthChecks_LiveAndReadyAllowAnonymous()
    {
        using var app = BuildAppWithHealthChecks();

        foreach (var path in new[] { HealthCheckExtensions.LiveEndpoint, HealthCheckExtensions.ReadyEndpoint })
        {
            var endpoint = RouteEndpoints(app).Single(e => e.RoutePattern.RawText == path);
            endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>().ShouldNotBeNull($"for {path}");
        }
    }

    [Fact]
    public void MapApplicationHealthChecks_DetailsRequiresAdminRole()
    {
        using var app = BuildAppWithHealthChecks();

        var endpoint = RouteEndpoints(app)
            .Single(e => e.RoutePattern.RawText == HealthCheckExtensions.DetailsEndpoint);
        var policy = endpoint.Metadata.GetMetadata<AuthorizationPolicy>();
        policy.ShouldNotBeNull();
        policy!.Requirements.OfType<RolesAuthorizationRequirement>()
            .SelectMany(r => r.AllowedRoles)
            .ShouldContain("Admin");
    }

    [Fact]
    public void MapApplicationHealthChecks_ReturnsSameApplication()
    {
        using var app = BuildAppWithHealthChecks();

        app.MapApplicationHealthChecks().ShouldBeSameAs(app);
    }

    [Fact]
    public void HealthCheckConstants_HaveExpectedValues()
    {
        HealthCheckExtensions.LiveEndpoint.ShouldBe("/health/live");
        HealthCheckExtensions.ReadyEndpoint.ShouldBe("/health/ready");
        HealthCheckExtensions.DetailsEndpoint.ShouldBe("/health/details");
        HealthCheckExtensions.TagLive.ShouldBe("live");
        HealthCheckExtensions.TagReady.ShouldBe("ready");
        HealthCheckExtensions.TagCritical.ShouldBe("critical");
    }
}
