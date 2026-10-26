using System.Reflection;
using Application.Common.DependencyInjection;
using Infrastructure.Common.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using ICurrentUserService = Application.Common.Interfaces.ICurrentUserService;
using BusinessMetrics = SharedContracts.Diagnostics.BusinessMetrics;
using FeatureFlags = SharedContracts.FeatureManagement.FeatureFlags;
using ApplicationCommand = Application.Common.Interfaces.ICommand;
using DbContextType = Infrastructure.Persistence.Context.DBContext;
using IElasticBulkService = Application.Search.Contracts.IElasticBulkService;
using IElasticDeadLetterQueue = Application.Search.Contracts.IElasticDeadLetterQueue;
using IRateLimitService = Application.Security.Contracts.IRateLimitService;
using ISearchService = Application.Search.Contracts.ISearchService;
using ISearchStatsService = Application.Search.Contracts.ISearchStatsService;
using Tests.TestInfrastructure.Temp;

namespace Tests.Architecture;

/// <summary>
/// Builds the production service container and resolves every service the
/// application itself registers.
///
/// A missing registration is invisible until the feature that needs it is used at
/// runtime, which is how the rate-limit and search services stayed unbound while the
/// application started happily. This test turns that class of defect into a build
/// failure.
/// </summary>
public class DependencyInjectionResolutionTests
{
    private static readonly Assembly[] ApplicationAssemblies =
    [
        typeof(ApplicationCommand).Assembly,
        typeof(DbContextType).Assembly
    ];

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ServiceContainer_ResolvesEveryApplicationService(bool useRedis, bool enableElasticsearch)
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(useRedis, enableElasticsearch);
        var environment = new TestHostEnvironment();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddSingleton<System.Diagnostics.DiagnosticListener>(
            new System.Diagnostics.DiagnosticListener("Microsoft.AspNetCore"));
        services.AddHttpContextAccessor();
        services.AddFeatureManagement(configuration.GetSection("FeatureManagement"));
        services.AddScoped<ICurrentUserService, CurrentUserServiceStub>();

        services.AddApplicationServices(configuration);
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();

        var failures = new List<string>();

        foreach (var descriptor in services)
        {
            if (!IsApplicationService(descriptor.ServiceType))
                continue;

            try
            {
                _ = scope.ServiceProvider.GetRequiredService(descriptor.ServiceType);
            }
            catch (Exception ex)
            {
                failures.Add($"{descriptor.ServiceType.Name}: {ex.GetBaseException().Message}");
            }
        }

        failures.ShouldBeEmpty();
    }

    [Fact]
    public void RateLimitService_IsRegisteredInBothCacheModes()
    {
        foreach (var useRedis in new[] { false, true })
        {
            var services = new ServiceCollection();
            var configuration = BuildConfiguration(useRedis, enableElasticsearch: false);
            var environment = new TestHostEnvironment();

            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IHostEnvironment>(environment);
            services.AddSingleton<System.Diagnostics.DiagnosticListener>(
                new System.Diagnostics.DiagnosticListener("Microsoft.AspNetCore"));
        services.AddHttpContextAccessor();
        services.AddFeatureManagement(configuration.GetSection("FeatureManagement"));
        services.AddSingleton<BusinessMetrics>();
        services.AddScoped<ICurrentUserService, CurrentUserServiceStub>();


            services.AddApplicationServices(configuration);
            services.AddInfrastructure(configuration);

            using var provider = services.BuildServiceProvider();

            provider.GetRequiredService<IRateLimitService>()
                .ShouldNotBeNull($"IRateLimitService must be resolvable (Cache:UseRedis={useRedis}).");
        }
    }

    [Fact]
    public void SearchServices_AreResolvableWhenElasticsearchIsEnabled()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(useRedis: false, enableElasticsearch: true);
        var environment = new TestHostEnvironment();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddSingleton<System.Diagnostics.DiagnosticListener>(
            new System.Diagnostics.DiagnosticListener("Microsoft.AspNetCore"));
        services.AddHttpContextAccessor();
        services.AddFeatureManagement(configuration.GetSection("FeatureManagement"));
        services.AddScoped<ICurrentUserService, CurrentUserServiceStub>();

        services.AddApplicationServices(configuration);
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISearchService>().ShouldNotBeNull();
        provider.GetRequiredService<IElasticBulkService>().ShouldNotBeNull();
        provider.GetRequiredService<ISearchStatsService>().ShouldNotBeNull();
        provider.GetRequiredService<IElasticDeadLetterQueue>().ShouldNotBeNull();
    }

    private static bool IsApplicationService(Type serviceType)
    {
        var assembly = serviceType.Assembly;

        return ApplicationAssemblies.Any(known => known == assembly)
               || serviceType.Namespace?.StartsWith("Application", StringComparison.Ordinal) == true
               || serviceType.Namespace?.StartsWith("Infrastructure", StringComparison.Ordinal) == true;
    }

    private static IConfiguration BuildConfiguration(bool useRedis, bool enableElasticsearch)
    {
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=ledka;Username=postgres;Password=postgres",
                ["ConnectionStrings:Redis"] = "",
                ["Cache:UseRedis"] = useRedis ? "true" : "false",
                ["Cache:RedisConnectionString"] = "localhost:6379",
                ["Jwt:Key"] = "test-signing-key-that-is-at-least-32-bytes-long!!",
                ["Jwt:Issuer"] = "ledka",
                ["Jwt:Audience"] = "ledka-clients",
                ["Jwt:AccessTokenMinutes"] = "30",
                ["Auth:OtpPepper"] = "test-otp-pepper-at-least-16-chars",
                ["Auth:OtpWindowMinutes"] = "5",
                ["Auth:MaxOtpAttempts"] = "5",
                ["Auth:SessionLifetimeDays"] = "30",
                ["Storage:Provider"] = "s3",
                ["Storage:BucketName"] = "test-bucket",
                ["Storage:AccessKey"] = "test-access-key",
                ["Storage:SecretKey"] = "test-secret-key",
                ["Storage:Endpoint"] = "http://localhost:9000",
                ["Storage:Region"] = "us-east-1",
                ["Storage:UseHttp"] = "true",
                ["Storage:ForcePathStyle"] = "true",
                ["Storage:MaxFileSizeBytes"] = "10485760",
                ["Storage:BaseUrl"] = "http://localhost:9000",
                ["Kavenegar:ApiKey"] = "test-kavenegar-key",
                ["Kavenegar:Sender"] = "100000000",
                ["Kavenegar:OtpTemplate"] = "test-template",
                ["Zarinpal:MerchantId"] = "00000000-0000-0000-0000-000000000000",
                ["Zarinpal:UseSandbox"] = "true",
                ["Security:AllowedOrigins:0"] = "https://localhost:5173",
                ["Security:AdminIpWhitelist:0"] = "127.0.0.1",
                ["Elasticsearch:IsEnabled"] = enableElasticsearch ? "true" : "false",
                ["Elasticsearch:Url"] = "http://localhost:9200",
                ["Elasticsearch:DefaultIndex"] = "products",
                ["OpenTelemetry:OtlpEndpoint"] = string.Empty,
                ["Swagger:Enabled"] = "false",
                ["Audit:ArchivePath"] = TestTempDirectory.CreateRoot("ledka-architecture-tests")
            });

        foreach (var flag in FeatureFlags.All)
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"FeatureManagement:{flag}"] = "false"
            });

        return builder.Build();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Ledka.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class CurrentUserServiceStub : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? SessionId => null;
        public bool IsAuthenticated => false;
        public bool IsAdmin => false;
        public string? IpAddress => null;
        public string? UserAgent => null;
        public string? GuestToken => null;
        public string FrontendBaseUrl => "https://localhost";
    }
}
