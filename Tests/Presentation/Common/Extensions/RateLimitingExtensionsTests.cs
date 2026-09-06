using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class RateLimitingExtensionsTests
{
    [Fact]
    public void AddApplicationRateLimiting_SetsRejectionStatusCode()
    {
        var services = new ServiceCollection();
        services.AddApplicationRateLimiting();

        services.BuildServiceProvider()
            .GetRequiredService<IOptions<RateLimiterOptions>>().Value
            .RejectionStatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public void AddApplicationRateLimiting_RegistersAdminWalletPolicy()
    {
        var services = new ServiceCollection();
        services.AddApplicationRateLimiting();

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        RateLimitingExtensions.AdminWalletPolicy.ShouldBe("admin-wallet");
        options.OnRejected.ShouldNotBeNull();
    }

    [Fact]
    public void AddApplicationRateLimiting_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddApplicationRateLimiting().ShouldBeSameAs(services);
    }

    [Fact]
    public void UseApplicationRateLimiter_ReturnsSameBuilder()
    {
        var services = new ServiceCollection();
        services.AddApplicationRateLimiting();
        var app = new ApplicationBuilder(services.BuildServiceProvider());

        app.UseApplicationRateLimiter().ShouldBeSameAs(app);
    }
}
