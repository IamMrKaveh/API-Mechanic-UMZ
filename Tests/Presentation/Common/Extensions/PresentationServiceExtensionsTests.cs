using Application.Auth.Contracts;
using Application.Common.Interfaces;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Auth.Services;
using Presentation.Common.Extensions;
using Presentation.Common.Filters;
using Presentation.Common.Services;
using SharedKernel.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class PresentationServiceExtensionsTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:AllowedOrigins:0"] = "https://app.example.com"
            })
            .Build();

    private static IHostEnvironment BuildEnvironment(string name)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(name);
        return env;
    }

    [Fact]
    public void AddPresentation_RegistersInternalServices()
    {
        var services = new ServiceCollection();
        services.AddPresentation(BuildConfig(), BuildEnvironment(Environments.Development));

        services.ShouldContain(s =>
            s.ServiceType == typeof(ICurrentUserService) &&
            s.ImplementationType == typeof(CurrentUserService) &&
            s.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(s =>
            s.ServiceType == typeof(IGoogleAuthenticationService) &&
            s.ImplementationType == typeof(HttpGoogleAuthenticationService) &&
            s.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(s =>
            s.ServiceType == typeof(OtpRateLimitFilter) &&
            s.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(s =>
            s.ServiceType == typeof(ReviewRateLimitFilter) &&
            s.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(s =>
            s.ServiceType == typeof(IMapper) &&
            s.ImplementationType!.FullName == "MapsterMapper.ServiceMapper");
        services.ShouldContain(s =>
            s.ServiceType == typeof(IPersianTextNormalizer) &&
            s.ImplementationType == typeof(PersianTextNormalizerService) &&
            s.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddPresentation_RegistersCorsPolicy()
    {
        var services = new ServiceCollection();
        services.AddPresentation(BuildConfig(), BuildEnvironment(Environments.Development));

        services.ShouldContain(s => s.ServiceType.FullName!.Contains("CorsService"));
    }

    [Fact]
    public void AddPresentation_WithoutAllowedOrigins_Throws()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() => services.AddPresentation(
            new ConfigurationBuilder().Build(), BuildEnvironment(Environments.Development)));
    }

    [Fact]
    public void AddPresentation_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddPresentation(BuildConfig(), BuildEnvironment(Environments.Development))
            .ShouldBeSameAs(services);
    }
}
