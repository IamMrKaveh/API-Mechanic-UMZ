using Amazon.S3;
using Application.Common.Contracts;
using Application.Common.Interfaces;
using Application.Localization.Contracts;
using Application.Media.Contracts;
using Application.Payment.Contracts;
using Application.Search.Contracts;
using Application.Security.Contracts;
using Application.Storage.Contracts;
using Domain.Audit.Interfaces;
using Domain.Payment.Interfaces;
using Domain.Product.Interfaces;
using Infrastructure.Audit.Services;
using Infrastructure.Audit.Storage;
using Infrastructure.BackgroundJobs.Services;
using Infrastructure.Cache.Redis.Lock;
using Infrastructure.Cache.Services;
using Infrastructure.Common.DependencyInjection;
using Infrastructure.Localization.Services;
using Infrastructure.Payment.Factory;
using Infrastructure.Payment.Repositories;
using Infrastructure.Payment.Services;
using Infrastructure.Persistence;
using Infrastructure.Product.Repositories;
using Infrastructure.Search.Contracts;
using Infrastructure.Search.Services;
using Infrastructure.Security.Services;
using Infrastructure.Storage.Options;
using Infrastructure.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Tests.Infrastructure.Common.DependencyInjection;

public class InfrastructureServiceExtensionsTests
{
    private static Dictionary<string, string?> ValidConfig() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=u;Password=p",
        ["Storage:Provider"] = "MinIO",
        ["Storage:BucketName"] = "bucket",
        ["Storage:BaseUrl"] = "https://cdn.example.com",
        ["Storage:Endpoint"] = "https://s3.example.com",
        ["Storage:AccessKey"] = "ak",
        ["Storage:SecretKey"] = "sk",
        ["Storage:Region"] = "us-east-1"
    };

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static void ShouldContain(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime)
    {
        services.ShouldContain(s =>
            s.ServiceType == serviceType &&
            s.ImplementationType == implementationType &&
            s.Lifetime == lifetime,
            $"registration {serviceType.Name} -> {implementationType.Name} ({lifetime})");
    }

    [Fact]
    public void AddInfrastructure_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddInfrastructure(BuildConfig(ValidConfig())).ShouldBeSameAs(services);
    }

    [Fact]
    public void AddInfrastructure_WithoutMemoryCache_RegistersMemoryCacheStack()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(ValidConfig()));

        ShouldContain(services, typeof(ICacheService), typeof(InMemoryCacheService), ServiceLifetime.Scoped);
        ShouldContain(services, typeof(IDistributedLock), typeof(NoOpDistributedLock), ServiceLifetime.Singleton);
        ShouldContain(services, typeof(IRateLimitService), typeof(InMemoryRateLimitService), ServiceLifetime.Scoped);
        ShouldContain(services, typeof(IIdempotencyService), typeof(CacheIdempotencyService), ServiceLifetime.Scoped);
        ShouldContain(services, typeof(ICacheInvalidationService), typeof(CacheInvalidationService), ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_RegistersCoreAndDomainServices()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(ValidConfig()));

        ShouldContain(services, typeof(IAuditService), typeof(AuditService), ServiceLifetime.Scoped);
        ShouldContain(services, typeof(IUnitOfWork), typeof(UnitOfWork), ServiceLifetime.Scoped);
        ShouldContain(services, typeof(ISqlConnectionFactory), typeof(SqlConnectionFactory), ServiceLifetime.Singleton);
        ShouldContain(services, typeof(ILocalizedErrorMessageProvider), typeof(LocalizedErrorMessageProvider), ServiceLifetime.Singleton);
        ShouldContain(services, typeof(IPaymentService), typeof(PaymentService), ServiceLifetime.Scoped);
        ShouldContain(services, typeof(IPaymentGatewayFactory), typeof(PaymentGatewayFactory), ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_RegistersRepositoriesByScan()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(ValidConfig()));

        ShouldContain(services, typeof(IPaymentTransactionRepository), typeof(PaymentRepository), ServiceLifetime.Scoped);
        ShouldContain(services, typeof(IProductRepository), typeof(ProductRepository), ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_WithS3CompatibleProvider_RegistersS3Storage()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(ValidConfig()));

        ShouldContain(services, typeof(IStorageService), typeof(S3FileStorageService), ServiceLifetime.Scoped);
        services.ShouldContain(s =>
            s.ServiceType == typeof(IAmazonS3) && s.Lifetime == ServiceLifetime.Singleton);
    }

    [Theory]
    [InlineData("s3")]
    [InlineData("AWS")]
    [InlineData("Arvan")]
    [InlineData("Liara")]
    [InlineData("minio")]
    public void AddInfrastructure_WithSupportedProviders_DoesNotThrow(string provider)
    {
        var values = ValidConfig();
        values["Storage:Provider"] = provider;
        var services = new ServiceCollection();

        Should.NotThrow(() => services.AddInfrastructure(BuildConfig(values)));
    }

    [Fact]
    public void AddInfrastructure_WithAntivirusDisabled_RegistersNullScanner()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(ValidConfig()));

        ShouldContain(services, typeof(IFileScanningService), typeof(NullFileScanningService), ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddInfrastructure_WithAntivirusEnabled_RegistersClamAvScanner()
    {
        var values = ValidConfig();
        values["Storage:Antivirus:IsEnabled"] = "true";
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(values));

        ShouldContain(services, typeof(IFileScanningService), typeof(ClamAvFileScanningService), ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_WithSearchDisabled_RegistersNoOpSearch()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(ValidConfig()));

        ShouldContain(services, typeof(ISearchService), typeof(NoOpSearchService), ServiceLifetime.Scoped);
        ShouldContain(services, typeof(IElasticsearchIndexer), typeof(NoOpElasticsearchIndexer), ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_WithSearchEnabled_RegistersResilientSearch()
    {
        var values = ValidConfig();
        values["Elasticsearch:IsEnabled"] = "true";
        values["Elasticsearch:Url"] = "http://localhost:9200";
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(values));

        ShouldContain(services, typeof(ISearchService), typeof(ResilientElasticSearchService), ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_WithoutArchiveProvider_RegistersFileSystemArchive()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(ValidConfig()));

        ShouldContain(services, typeof(IAuditArchiveStorage), typeof(FileSystemAuditArchiveStorage), ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_WithS3ArchiveProvider_RegistersS3Archive()
    {
        var values = ValidConfig();
        values["Storage:AuditArchive:Provider"] = "S3";
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(values));

        ShouldContain(services, typeof(IAuditArchiveStorage), typeof(S3AuditArchiveStorage), ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInfrastructure_WithRedisCache_RegistersDistributedImplementations()
    {
        var values = ValidConfig();
        values["Cache:UseRedis"] = "true";
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(values));

        services.ShouldContain(s => s.ServiceType == typeof(IDistributedLock));
        var cacheDescriptor = services.First(s => s.ServiceType == typeof(ICacheService));
        cacheDescriptor.ImplementationFactory.ShouldNotBeNull();
        ShouldContain(services, typeof(IDistributedLock), typeof(DistributedLockService), ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddInfrastructure_BindsStorageOptions()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(ValidConfig()));

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<StorageOptions>>().Value;

        options.Provider.ShouldBe("MinIO");
        options.BucketName.ShouldBe("bucket");
        options.Endpoint.ShouldBe("https://s3.example.com");
    }

    [Fact]
    public void AddInfrastructure_RegistersHealthCheckService()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(BuildConfig(ValidConfig()));

        services.BuildServiceProvider()
            .GetRequiredService<HealthCheckService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddInfrastructure_WithoutDefaultConnection_Throws()
    {
        var values = ValidConfig();
        values.Remove("ConnectionStrings:DefaultConnection");
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddInfrastructure(BuildConfig(values)));

        ex.Message.ShouldContain("DefaultConnection");
    }

    [Fact]
    public void AddInfrastructure_WithoutStorageSection_Throws()
    {
        var values = ValidConfig();
        foreach (var key in values.Keys.Where(k => k.StartsWith("Storage:")).ToList())
            values.Remove(key);
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddInfrastructure(BuildConfig(values)));
    }

    [Fact]
    public void AddInfrastructure_WithUnsupportedStorageProvider_Throws()
    {
        var values = ValidConfig();
        values["Storage:Provider"] = "Ftp";
        var services = new ServiceCollection();

        var ex = Should.Throw<NotSupportedException>(() =>
            services.AddInfrastructure(BuildConfig(values)));

        ex.Message.ShouldContain("Ftp");
    }
}
