using Amazon.Runtime;
using Application.Attribute.Adapters;
using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Application.Brand.Adapters;
using Application.Common.Options;
using Application.Discount.Contracts;
using Application.Location.Contracts;
using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Payment.Contracts;
using Domain.Attribute.Interfaces;
using Domain.Brand.Interfaces;
using Domain.Payment.Interfaces;
using Domain.Review.Interfaces;
using Domain.Wallet.FraudDetection;
using Domain.Wallet.FraudDetection.Rules;
using Infrastructure.Audit.Services;
using Infrastructure.Audit.Storage;
using Infrastructure.Auth.Options;
using Infrastructure.Auth.Services;
using Infrastructure.BackgroundJobs;
using Infrastructure.BackgroundJobs.Options;
using Infrastructure.BackgroundJobs.Services;
using Infrastructure.Cache.Redis.Lock;
using Infrastructure.Cache.Redis.Services;
using Infrastructure.Cache.Services;
using Infrastructure.Common.DependencyInjection;
using Infrastructure.Common.Options;
using Infrastructure.Common.Services;
using Infrastructure.Communication.Options;
using Infrastructure.Communication.Services;
using Infrastructure.DataProtection.Repositories;
using Infrastructure.Discount.Services;
using Infrastructure.Inventory.Services;
using Infrastructure.Location.Services;
using Infrastructure.Media.Services;
using Infrastructure.Notification.Services;
using Infrastructure.Order.Seeders;
using Infrastructure.Order.Services;
using Infrastructure.Order.Services.Strategies;
using Infrastructure.Payment.Factory;
using Infrastructure.Payment.Repositories;
using Infrastructure.Payment.Seeders;
using Infrastructure.Payment.Services;
using Infrastructure.Payment.ZarinPal;
using Infrastructure.Payment.ZarinPal.Options;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Infrastructure.Product.QueryServices;
using Infrastructure.Product.Repositories;
using Infrastructure.Review.Services;
using Infrastructure.Search;
using Infrastructure.Search.Contracts;
using Infrastructure.Search.Options;
using Infrastructure.Search.Services;
using Infrastructure.Security.Services;
using Infrastructure.Storage.Options;
using Infrastructure.Storage.Services;
using Scrutor;
using DateTimeProvider = Infrastructure.Common.Services.DateTimeProvider;
using IAuditArchiveStorage = Domain.Audit.Interfaces.IAuditArchiveStorage;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Infrastructure.Common.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddCaching(configuration);
        services.AddInfrastructureCoreServices();
        services.AddRepositories();
        services.AddQueryServices();
        services.AddDomainServices();
        services.AddAuthServices();
        services.AddPaymentServices(configuration);
        services.AddStorageServices(configuration);
        services.AddCommunicationServices(configuration);
        services.AddSearchServices(configuration);
        services.AddBackgroundServices(configuration);
        services.AddHealthChecks(configuration);
        services.AddDataProtectionLayer(configuration);
        services.AddJwtAuthentication();

        return services;
    }

    private static void AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CacheOptions>()
            .Bind(configuration.GetSection(CacheOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>()
            ?? new CacheOptions();

        if (cacheOptions.UseRedis)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis")
                ?? configuration["Cache:RedisConnectionString"]
                ?? "localhost:6379";

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = cacheOptions.KeyPrefix;
            });

            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddSingleton<IDistributedLock, DistributedLockService>();
            services.AddScoped<IRateLimitService, RateLimitService>();
            services.AddScoped<IIdempotencyService, RedisIdempotencyService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddScoped<ICacheService, InMemoryCacheService>();
            services.AddSingleton<IDistributedLock, NoOpDistributedLock>();
            services.AddScoped<IRateLimitService, InMemoryRateLimitService>();
            services.AddScoped<IIdempotencyService, CacheIdempotencyService>();
        }

        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
    }

    private static void AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<DomainEventInterceptor>();

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isDevelopment = string.Equals(
            environment,
            Environments.Development,
            StringComparison.OrdinalIgnoreCase);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection connection string was not found.");

        services.AddDbContext<DBContext>((sp, options) =>
        {
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(DBContext).Assembly.FullName);
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);

                    if (!isDevelopment)
                    {
                        npgsql.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null);
                    }
                });

            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<DomainEventInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ProductRepository>()
            .AddClasses(classes => classes
                .Where(type =>
                    type.Name.EndsWith("Repository", StringComparison.Ordinal) &&
                    type != typeof(PaymentRepository)))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsMatchingInterface()
                .WithScopedLifetime());

        services.AddScoped<IAttributeTypeUniquenessChecker, AttributeTypeUniquenessCheckerAdapter>();
        services.AddScoped<IPaymentTransactionRepository, PaymentRepository>();
    }

    private static void AddQueryServices(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ProductQueryService>()
            .AddClasses(classes => classes
                .Where(type => type.Name.EndsWith("QueryService", StringComparison.Ordinal)))
                .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                .AsMatchingInterface()
                .WithScopedLifetime());
    }

    private static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditArchiveStorage, FileSystemAuditArchiveStorage>();
        services.AddScoped<IAuditMaskingService, AuditMaskingService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IBrandUniquenessChecker, BrandUniquenessCheckerAdapter>();
        services.AddScoped<ICheckoutOrchestrationService, CheckoutOrchestrationService>();
        services.AddScoped<ICheckoutAddressResolverService, CheckoutAddressResolverService>();
        services.AddScoped<ICheckoutCartItemBuilderService, CheckoutCartItemBuilderService>();
        services.AddScoped<ICheckoutDiscountApplicatorService, CheckoutDiscountApplicatorService>();
        services.AddScoped<ICheckoutOrderCreationService, CheckoutOrderCreationService>();
        services.AddScoped<ICheckoutPaymentProcessorService, CheckoutPaymentProcessorService>();
        services.AddScoped<ICheckoutPaymentStrategy, CashOnDeliveryCheckoutPaymentStrategy>();
        services.AddScoped<ICheckoutPaymentStrategy, WalletCheckoutPaymentStrategy>();
        services.AddScoped<ICheckoutPaymentStrategy, ZarinPalCheckoutPaymentStrategy>();
        services.AddScoped<ICheckoutPaymentStrategyResolver, CheckoutPaymentStrategyResolver>();
        services.AddScoped<ICheckoutPriceValidatorService, CheckoutPriceValidatorService>();
        services.AddScoped<ICheckoutShippingValidatorService, CheckoutShippingValidatorService>();
        services.AddScoped<ICheckoutStockValidatorService, CheckoutStockValidatorService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IFraudDetectionRule, HighVelocityRule>();
        services.AddScoped<IFraudDetectionRule, UnusualAmountRule>();
        services.AddScoped<IFraudDetectionRule, RapidTopUpWithdrawRule>();
        services.AddScoped<IFraudDetectionRule, MultipleFailedTopUpRule>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPurchaseVerificationService, PurchaseVerificationService>();
    }

    private static void AddAuthServices(this IServiceCollection services)
    {
        services.AddOptions<OtpOptions>()
            .BindConfiguration(OtpOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<InitialAdminOptions>()
            .BindConfiguration(IInitialAdminOptions.SectionName)
            .ValidateOnStart();

        services.AddSingleton<IInitialAdminOptions>(sp =>
            sp.GetRequiredService<IOptions<InitialAdminOptions>>().Value);

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
    }

    private static void AddStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMediaService, MediaService>();

        var storageSection = configuration.GetSection(StorageOptions.SectionName);

        services.AddOptions<StorageOptions>()
            .Bind(storageSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var storageOptions = storageSection.Get<StorageOptions>()
            ?? throw new InvalidOperationException(
                $"Storage configuration section '{StorageOptions.SectionName}' is missing.");

        if (string.IsNullOrWhiteSpace(storageOptions.Provider))
            throw new InvalidOperationException("Storage provider is not configured.");

        if (string.IsNullOrWhiteSpace(storageOptions.BucketName))
            throw new InvalidOperationException("Storage bucket name is not configured.");

        var provider = storageOptions.Provider.Trim().ToLowerInvariant();

        switch (provider)
        {
            case "s3":
            case "aws":
            case "arvan":
            case "liara":
            case "minio":
                services.AddSingleton<IAmazonS3>(sp =>
                {
                    var opts = sp.GetRequiredService<IOptions<StorageOptions>>().Value;

                    var config = new AmazonS3Config
                    {
                        ServiceURL = opts.Endpoint,
                        ForcePathStyle = opts.ForcePathStyle,
                        AuthenticationRegion = opts.Region,
                        UseHttp = opts.UseHttp,
                        RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                        ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
                    };

                    return new AmazonS3Client(opts.AccessKey, opts.SecretKey, config);
                });

                services.AddScoped<IStorageService, S3FileStorageService>();
                break;

            default:
                throw new NotSupportedException(
                    $"Storage provider '{storageOptions.Provider}' is not supported. " +
                    $"Supported providers: S3, AWS, Arvan, Liara, MinIO.");
        }
    }

    private static void AddCommunicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<ILocationService, LocationService>(client =>
        {
            client.BaseAddress = new Uri("https://iran-locations-api.ir/api/v1/fa/");
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        });

        services.AddOptions<KavenegarOptions>()
            .Bind(configuration.GetSection(KavenegarOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ISmsService, SmsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        })
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
            .AddTransientHttpErrorPolicy(policy =>
                policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
    }

    private static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ZarinPalOptions>()
            .Bind(configuration.GetSection(ZarinPalOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FrontendUrlsOptions>()
            .Bind(configuration.GetSection(FrontendUrlsOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<ApiBaseUrlOptions>()
            .Bind(configuration.GetSection(ApiBaseUrlOptions.SectionName))
            .ValidateOnStart();

        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddHttpClient<ZarinPalPaymentGateway>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<ZarinPalOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(opts.ApiBaseUrl)
                ? "https://api.zarinpal.com/pg/v4/payment/"
                : opts.ApiBaseUrl;
            if (!baseUrl.EndsWith('/')) baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds > 0 ? opts.TimeoutSeconds : 30);
        })
        .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(2, i => TimeSpan.FromSeconds(i)))
        .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(60)));

        services.AddHttpClient("ZarinPalSandbox", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<ZarinPalOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(opts.SandboxApiBaseUrl)
                ? "https://sandbox.zarinpal.com/"
                : opts.SandboxApiBaseUrl;
            if (!baseUrl.EndsWith('/')) baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds > 0 ? opts.TimeoutSeconds : 30);
        })
        .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(2, i => TimeSpan.FromSeconds(i)));

        services.AddScoped<ZarinPalPaymentGateway>();
        services.AddScoped<ZarinPalSandboxGateway>();

        services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<ZarinPalPaymentGateway>());
        services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<ZarinPalSandboxGateway>());

        return services;
    }

    private static void AddSearchServices(this IServiceCollection services, IConfiguration configuration)
    {
        var elasticsearchSection = configuration.GetSection(ElasticsearchOptions.SectionName);

        services.AddOptions<ElasticsearchOptions>()
            .Bind(elasticsearchSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = elasticsearchSection.Get<ElasticsearchOptions>();

        if (options is not null && options.IsEnabled)
        {
            services.AddSingleton<ElasticsearchClient>(sp =>
            {
                var settings = new ElasticsearchClientSettings(
                    new Uri(options.Url))
                    .DefaultIndex(options.DefaultIndex);

                return new ElasticsearchClient(settings);
            });

            services.AddScoped<IElasticsearchIndexer, ElasticsearchIndexer>();

            services.AddScoped<ISearchService, ResilientElasticSearchService>();
            services.AddScoped<IElasticIndexManager, ElasticIndexManager>();
            services.AddScoped<ISearchDatabaseSyncService, ElasticsearchDatabaseSyncService>();
        }
        else
        {
            services.AddScoped<IElasticsearchIndexer, NoOpElasticsearchIndexer>();
            services.AddScoped<ISearchService, NoOpSearchService>();
            services.AddScoped<IElasticIndexManager, NoOpElasticIndexManager>();
            services.AddScoped<ISearchDatabaseSyncService, NoOpSearchDatabaseSyncService>();
        }
    }

    private static void AddBackgroundServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuditArchiveStorage, S3AuditArchiveStorage>();

        services.AddHostedService<AuditRetentionJob>();
        services.AddHostedService<ExpiredOrderCleanupJob>();
        services.AddHostedService<ExpiredSessionCleanupJob>();
        services.AddHostedService<FraudDetectionJob>();
        services.AddHostedService<InventoryReservationExpiryJob>();
        services.AddHostedService<OrderStatusSeeder>();
        services.AddHostedService<OrphanedFileCleanupJob>();
        services.AddHostedService<OutboxProcessingJob>();
        services.AddHostedService<PaymentCleanupJob>();
        services.AddHostedService<PaymentMethodSeeder>();
        services.AddHostedService<PaymentReconciliationJob>();
        services.AddHostedService<WalletReconciliationJob>();
        services.AddHostedService<WalletReservationExpiryJob>();
        services.AddHostedService<WalletTopUpCleanupJob>();

        var elasticsearchSection = configuration.GetSection(ElasticsearchOptions.SectionName);
        var options = elasticsearchSection.Get<ElasticsearchOptions>();

        if (options is not null && options.IsEnabled)
        {
            services.AddHostedService<ElasticsearchOutboxJob>();

            if (options.EnableBackgroundSync)
                services.AddHostedService<ElasticsearchSyncJob>();
        }

        services.AddOptions<ReservationExpiryOptions>()
            .BindConfiguration(ReservationExpiryOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>()
            ?? new CacheOptions();

        var builder = services.AddHealthChecks()
            .AddCheck("postgresql", new NpgsqlHealthCheck(connectionString),
                tags: ["db", "sql", "postgresql"]);

        if (cacheOptions.UseRedis)
        {
            builder.AddRedis(
                configuration.GetConnectionString("Redis") ?? "localhost:6379",
                name: "redis",
                tags: ["cache", "redis"]);
        }
    }

    private static void AddDataProtectionLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>()
            ?? new CacheOptions();

        if (!cacheOptions.UseRedis)
        {
            services.AddDataProtection();
            return;
        }

        services.AddDataProtection().Services.AddSingleton<IXmlRepository>(provider =>
            new ResilientRedisXmlRepository(
                provider.GetRequiredService<IConnectionMultiplexer>(),
                provider.GetRequiredService<ILogger<ResilientRedisXmlRepository>>(),
                "DataProtection",
                TimeSpan.FromDays(90)));
    }

    private static void AddInfrastructureCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUrlResolverService, UrlResolverService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IOutboxEventTypeRegistry,
            OutboxEventTypeRegistry>();
    }

    private static void AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private sealed class NpgsqlHealthCheck(string connectionString) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(ex.Message);
            }
        }
    }
}