using Amazon.Runtime;
using Application.Analytics.Contracts;
using Application.Attribute.Adapters;
using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Application.Brand.Contracts;
using Application.Cart.Contracts;
using Application.Category.Contracts;
using Application.Discount.Contracts;
using Application.Location.Contracts;
using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Payment.Contracts;
using Application.Product.Contracts;
using Application.Review.Contracts;
using Application.Shipping.Contracts;
using Application.Support.Contracts;
using Application.User.Contracts;
using Application.Variant.Contracts;
using Application.Wishlist.Contracts;
using Domain.Attribute.Interfaces;
using Domain.Audit.Interfaces;
using Domain.Brand.Interfaces;
using Domain.Cart.Interfaces;
using Domain.Category.Interfaces;
using Domain.Discount.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Media.Interfaces;
using Domain.Notification.Interfaces;
using Domain.Order.Interfaces;
using Domain.Payment.Interfaces;
using Domain.Product.Interfaces;
using Domain.Review.Interfaces;
using Domain.Security.Interfaces;
using Domain.Shipping.Interfaces;
using Domain.Support.Interfaces;
using Domain.User.Interfaces;
using Domain.Variant.Interfaces;
using Domain.Wallet.FraudDetection;
using Domain.Wallet.FraudDetection.Rules;
using Domain.Wallet.Interfaces;
using Domain.Wishlist.Interfaces;
using Infrastructure.Analytics.QueryServices;
using Infrastructure.Attribute.Repositories;
using Infrastructure.Audit.QueryServices;
using Infrastructure.Audit.Repositories;
using Infrastructure.Audit.Services;
using Infrastructure.Auth.Options;
using Infrastructure.Auth.Repositories;
using Infrastructure.Auth.Services;
using Infrastructure.BackgroundJobs;
using Infrastructure.BackgroundJobs.Abstractions;
using Infrastructure.BackgroundJobs.Options;
using Infrastructure.BackgroundJobs.Services;
using Infrastructure.Brand.QueryServices;
using Infrastructure.Brand.Repositories;
using Infrastructure.Cache.Redis.Lock;
using Infrastructure.Cache.Redis.Services;
using Infrastructure.Cache.Services;
using Infrastructure.Cart.QueryServices;
using Infrastructure.Cart.Repositories;
using Infrastructure.Category.QueryServices;
using Infrastructure.Category.Repositories;
using Infrastructure.Common.DependencyInjection;
using Infrastructure.Common.Services;
using Infrastructure.Communication.Options;
using Infrastructure.Communication.Services;
using Infrastructure.DataProtection.Repositories;
using Infrastructure.Discount.QueryServices;
using Infrastructure.Discount.Repositories;
using Infrastructure.Discount.Services;
using Infrastructure.Inventory.QueryServices;
using Infrastructure.Inventory.Repositories;
using Infrastructure.Inventory.Services;
using Infrastructure.Location.Services;
using Infrastructure.Media.QueryServices;
using Infrastructure.Media.Repositories;
using Infrastructure.Media.Services;
using Infrastructure.Notification.QueryServices;
using Infrastructure.Notification.Repositories;
using Infrastructure.Notification.Services;
using Infrastructure.Order.QueryServices;
using Infrastructure.Order.Repositories;
using Infrastructure.Order.Seeders;
using Infrastructure.Order.Services;
using Infrastructure.Payment.Factory;
using Infrastructure.Payment.QueryServices;
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
using Infrastructure.Review.QueryServices;
using Infrastructure.Review.Repositories;
using Infrastructure.Review.Services;
using Infrastructure.Search;
using Infrastructure.Search.Contracts;
using Infrastructure.Search.Options;
using Infrastructure.Search.Services;
using Infrastructure.Security.Services;
using Infrastructure.Shipping.QueryServices;
using Infrastructure.Shipping.Repositories;
using Infrastructure.Storage.Options;
using Infrastructure.Storage.Services;
using Infrastructure.Support.QueryServices;
using Infrastructure.Support.Repositories;
using Infrastructure.User.QueryServices;
using Infrastructure.User.Repositories;
using Infrastructure.Variant.QueryServices;
using Infrastructure.Variant.Repositories;
using Infrastructure.Wallet.QueryServices;
using Infrastructure.Wallet.Repositories;
using Infrastructure.Wishlist.QueryServices;
using Infrastructure.Wishlist.Repositories;
using DateTimeProvider = Infrastructure.Common.Services.DateTimeProvider;
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
        services.AddAuthServices(configuration);
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
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

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
        }
        else
        {
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddScoped<ICacheService, InMemoryCacheService>();
            services.AddSingleton<IDistributedLock, NoOpDistributedLock>();
            services.AddScoped<IRateLimitService, InMemoryRateLimitService>();
        }

        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        services.AddScoped<IIdempotencyService, CacheIdempotencyService>();
    }

    private static void AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<DomainEventInterceptor>();

        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var isDevelopment =
            string.Equals(
                environment,
                Environments.Development,
                StringComparison.OrdinalIgnoreCase);

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection connection string was not found.");

        services.AddDbContext<DBContext>((sp, options) =>
        {
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(
                        typeof(DBContext).Assembly.FullName);

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
        services.AddScoped<IAttributeTypeUniquenessChecker, AttributeTypeUniquenessCheckerAdapter>();
        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderStatusRepository, OrderStatusRepository>();
        services.AddScoped<IOrderProcessStateRepository, OrderProcessStateRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IShippingRepository, ShippingRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVariantRepository, VariantRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWalletFraudAlertRepository, WalletFraudAlertRepository>();
        services.AddScoped<IWalletTopUpRepository, WalletTopUpRepository>();
        services.AddScoped<IWalletWithdrawalRepository, WalletWithdrawalRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
    }

    private static void AddQueryServices(this IServiceCollection services)
    {
        services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IBrandQueryService, BrandQueryService>();
        services.AddScoped<ICartQueryService, CartQueryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<IDiscountQueryService, DiscountQueryService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IMediaQueryService, MediaQueryService>();
        services.AddScoped<INotificationQueryService, NotificationQueryService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IOrderStatusQueryService, OrderStatusQueryService>();
        services.AddScoped<IPaymentQueryService, PaymentQueryService>();
        services.AddScoped<IPaymentMethodQueryService, PaymentMethodQueryService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IReviewQueryService, ReviewQueryService>();
        services.AddScoped<IShippingQueryService, ShippingQueryService>();
        services.AddScoped<IStockLedgerQueryService, StockLedgerQueryService>();
        services.AddScoped<ITicketQueryService, TicketQueryService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IVariantQueryService, VariantQueryService>();
        services.AddScoped<IWalletQueryService, WalletQueryService>();
        services.AddScoped<IWalletFraudAlertQueryService, WalletFraudAlertQueryService>();
        services.AddScoped<IWalletWithdrawalQueryService, WalletWithdrawalQueryService>();
        services.AddScoped<IWishlistQueryService, WishlistQueryService>();
    }

    private static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditMaskingService, AuditMaskingService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICheckoutOrchestrationService, CheckoutOrchestrationService>();
        services.AddScoped<ICheckoutAddressResolverService, CheckoutAddressResolverService>();
        services.AddScoped<ICheckoutCartItemBuilderService, CheckoutCartItemBuilderService>();
        services.AddScoped<ICheckoutDiscountApplicatorService, CheckoutDiscountApplicatorService>();
        services.AddScoped<ICheckoutOrderCreationService, CheckoutOrderCreationService>();
        services.AddScoped<ICheckoutPaymentProcessorService, CheckoutPaymentProcessorService>();
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
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPurchaseVerificationService, PurchaseVerificationService>();
    }

    private static void AddAuthServices(this IServiceCollection services, IConfiguration configuration)
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
        services.Configure<StorageOptions>(storageSection);

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

        services.Configure<KavenegarOptions>(configuration.GetSection(KavenegarOptions.SectionName));

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

    private static void AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ZarinPalOptions>()
            .BindConfiguration(ZarinPalOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddHttpClient<ZarinPalPaymentGateway>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<ZarinPalOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(opts.ApiBaseUrl)
                ? "https://payment.zarinpal.com/"
                : opts.ApiBaseUrl;
            if (!baseUrl.EndsWith('/')) baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds > 0 ? opts.TimeoutSeconds : 30);
        })
        .AddTransientHttpErrorPolicy(policy =>
            policy.WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(retryAttempt)))
        .AddTransientHttpErrorPolicy(policy =>
            policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(60)));

        services.AddHttpClient("ZarinPalSandbox", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<ZarinPalOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(opts.SandboxApiBaseUrl)
                ? "https://sandbox.zarinpal.com/"
                : opts.SandboxApiBaseUrl!;
            if (!baseUrl.EndsWith('/')) baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds > 0 ? opts.TimeoutSeconds : 30);
        })
        .AddTransientHttpErrorPolicy(policy =>
            policy.WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(retryAttempt)));

        services.AddScoped<ZarinPalSandboxGateway>();
        services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<ZarinPalSandboxGateway>());
        services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<ZarinPalPaymentGateway>());
    }

    private static void AddSearchServices(this IServiceCollection services, IConfiguration configuration)
    {
        var elasticsearchSection = configuration.GetSection(ElasticsearchOptions.SectionName);
        services.Configure<ElasticsearchOptions>(elasticsearchSection);

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