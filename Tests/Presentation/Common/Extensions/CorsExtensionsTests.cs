using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class CorsExtensionsTests
{
    private static IConfiguration BuildConfig(params string[] origins)
    {
        var data = origins.Select((o, i) =>
            new KeyValuePair<string, string?>($"Security:AllowedOrigins:{i}", o));
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    [Fact]
    public void AddCustomCors_WithoutOrigins_Throws()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddCustomCors(config));

        ex.Message.ShouldContain("Security:AllowedOrigins");
    }

    [Fact]
    public void AddCustomCors_WithEmptyOrigins_Throws()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddCustomCors(BuildConfig()));
    }

    [Fact]
    public void AddCustomCors_WithOrigins_RegistersAllowClientPolicy()
    {
        var services = new ServiceCollection();
        services.AddCustomCors(BuildConfig("https://app.example.com", "https://admin.example.com"));

        var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value
            .GetPolicy("AllowClient");

        policy.ShouldNotBeNull();
        policy!.Origins.ShouldBe(["https://app.example.com", "https://admin.example.com"], ignoreOrder: true);
        policy.Headers.ShouldContain("*");
        policy.Methods.ShouldContain("*");
        policy.SupportsCredentials.ShouldBeTrue();
    }

    [Fact]
    public void AddCustomCors_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddCustomCors(BuildConfig("https://app.example.com")).ShouldBeSameAs(services);
    }

    [Fact]
    public void UseCustomCors_ReturnsSameBuilder()
    {
        var services = new ServiceCollection();
        services.AddCustomCors(BuildConfig("https://app.example.com"));
        var app = new ApplicationBuilder(services.BuildServiceProvider());

        app.UseCustomCors().ShouldBeSameAs(app);
    }
}
