using Application.Analytics.Contracts;
using Application.Attribute.Contracts;
using Application.Auth.Contracts;
using Application.Brand.Contracts;
using Application.Cart.Contracts;
using Application.Category.Contracts;
using Application.Discount.Contracts;
using Application.Location.Contracts;
using Application.Payment.Contracts;
using Application.Product.Contracts;
using Application.Review.Contracts;
using Application.Security.Interfaces;
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
using Domain.Wallet.Interfaces;
using Domain.Wishlist.Interfaces;
using Infrastructure.Attribute.QueryServices;
using Infrastructure.Attribute.Repositories;
using Infrastructure.Audit.QueryServices;
using Infrastructure.Audit.Repositories;
using Infrastructure.Audit.Services;
using Infrastructure.Auth.Options;
using Infrastructure.Auth.Repositories;
using Infrastructure.Auth.Services;
using Infrastructure.BackgroundJobs;
using Infrastructure.Brand.QueryServices;
using Infrastructure.Brand.Repositories;
using Infrastructure.Cache.Redis.Services;
using Infrastructure.Cache.Services;
using Infrastructure.Cart.QueryServices;
using Infrastructure.Cart.Repositories;
using Infrastructure.Category.QueryServices;
using Infrastructure.Category.Repositories;
using Infrastructure.Common.Services;
using Infrastructure.Communication.Options;
using Infrastructure.Communication.Services;
using Infrastructure.Discount.QueryServices;
using Infrastructure.Discount.Repositories;
using Infrastructure.Discount.Services;
using Infrastructure.Inventory.BackgroundServices;
using Infrastructure.Inventory.QueryServices;
using Infrastructure.Inventory.Repositories;
using Infrastructure.Inventory.Services;
using Infrastructure.Location.Services;
using Infrastructure.Media.BackgroundServices;
using Infrastructure.Media.QueryServices;
using Infrastructure.Media.Repositories;
using Infrastructure.Media.Services;
using Infrastructure.Notification.QueryServices;
using Infrastructure.Notification.Repositories;
using Infrastructure.Notification.Services;
using Infrastructure.Order.QueryServices;
using Infrastructure.Order.Repositories;
using Infrastructure.Payment.BackgroundServices;
using Infrastructure.Payment.QueryServices;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Infrastructure.Product.QueryServices;
using Infrastructure.Product.Repositories;
using Infrastructure.Review.QueryServices;
using Infrastructure.Review.Repositories;
using Infrastructure.Search;
using Infrastructure.Search.BackgroundServices;
using Infrastructure.Search.Options;
using Infrastructure.Search.Services;
using Infrastructure.Security.Options;
using Infrastructure.Security.Services;
using Infrastructure.Security.Settings;
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
using Infrastructure.Wallet.Services;
using Infrastructure.Wishlist.QueryServices;
using Infrastructure.Wishlist.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;

namespace Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddRedisCache(configuration);
        services.AddRepositories();
        services.AddQueryServices();
        services.AddDomainServices();
        services.AddAuthServices(configuration);
        services.AddStorageServices(configuration);
        services.AddCommunicationServices(configuration);
        services.AddSearchServices(configuration);
        services.AddBackgroundServices(configuration);
        services.AddHealthChecks(configuration);
        services.AddDataProtectionWithRedis(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddHttpContextAccessor();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<DomainEventInterceptor>();

        services.AddDbContext<DBContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(DBContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                })
                .AddInterceptors(
                    sp.GetRequiredService<AuditableEntityInterceptor>(),
                    sp.GetRequiredService<DomainEventInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUrlResolverService, UrlResolverService>();
    }

    private static void AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Cache:RedisConnectionString"]
            ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = configuration["Cache:KeyPrefix"] ?? "shop";
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddSingleton<IDistributedLock, DistributedLockService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IVariantRepository, VariantRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderStatusRepository, OrderStatusRepository>();
        services.AddScoped<IOrderProcessStateRepository, OrderProcessStateRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IShippingRepository, ShippingRepository>();
        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
    }

    private static void AddQueryServices(this IServiceCollection services)
    {
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<IBrandQueryService, BrandQueryService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IStockLedgerQueryService, StockLedgerQueryService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IOrderStatusQueryService, OrderStatusQueryService>();
        services.AddScoped<ICartQueryService, CartQueryService>();
        services.AddScoped<IPaymentQueryService, PaymentQueryService>();
        services.AddScoped<IShippingQueryService, ShippingQueryService>();
        services.AddScoped<ITicketQueryService, TicketQueryService>();
        services.AddScoped<IWishlistQueryService, WishlistQueryService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
        services.AddScoped<IVariantQueryService, VariantQueryService>();
        services.AddScoped<IDiscountQueryService, DiscountQueryService>();
        services.AddScoped<IReviewQueryService, ReviewQueryService>();
        services.AddScoped<INotificationQueryService, NotificationQueryService>();
        services.AddScoped<IWalletQueryService, WalletQueryService>();
        services.AddScoped<IAttributeQueryService, AttributeQueryService>();
        services.AddScoped<IMediaQueryService, MediaQueryService>();
    }

    private static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditMaskingService, AuditMaskingService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IStockLedgerService, StockLedgerService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddHttpClient<LocationService>();
    }

    private static void AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IRateLimitService, RateLimitService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISessionService, SessionService>();
    }

    private static void AddStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LiaraStorageOptions>(configuration.GetSection(LiaraStorageOptions.SectionName));
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));

        var storageProvider = configuration["Storage:Provider"] ?? "Liara";

        if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAmazonS3>(_ =>
            {
                var options = configuration.GetSection(S3Options.SectionName).Get<S3Options>()!;
                var awsOptions = new Amazon.Runtime.BasicAWSCredentials(options.AccessKey, options.SecretKey);
                var config = new Amazon.S3.AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region) };
                return new Amazon.S3.AmazonS3Client(awsOptions, config);
            });
            services.AddScoped<IStorageService, S3FileStorageService>();
        }
        else
        {
            services.AddScoped<IStorageService, LiaraStorageService>();
        }
    }

    private static void AddCommunicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KavenegarOptions>(configuration.GetSection(KavenegarOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IEmailService, EmailService>();
    }

    private static void AddSearchServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));

        var elasticOptions = configuration.GetSection(ElasticsearchOptions.SectionName).Get<ElasticsearchOptions>()
            ?? new ElasticsearchOptions();

        if (elasticOptions.IsEnabled)
        {
            services.AddSingleton<ElasticsearchClient>(_ =>
            {
                var settings = new ElasticsearchClientSettings(new Uri(elasticOptions.Uri));
                if (!string.IsNullOrEmpty(elasticOptions.Username))
                    settings = settings.Authentication(new BasicAuthentication(elasticOptions.Username, elasticOptions.Password ?? string.Empty));
                return new ElasticsearchClient(settings);
            });

            services.AddSingleton<ElasticsearchCircuitBreaker>();
            services.AddSingleton<ElasticsearchMetrics>();
            services.AddScoped<ElasticSearchService>();
            services.AddScoped<ISearchService, ResilientElasticSearchService>();
            services.AddScoped<IElasticBulkService, ElasticBulkService>();
            services.AddScoped<IElasticIndexManager, ElasticIndexManager>();
            services.AddScoped<ISearchDatabaseSyncService, ElasticsearchDatabaseSyncService>();
            services.AddScoped<ISearchStatsService, ElasticsearchStatsService>();
            services.AddScoped<ElasticsearchDatabaseSyncService>();
        }
        else
        {
            services.AddSingleton<ElasticsearchClient>(_ =>
            {
                var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"));
                return new ElasticsearchClient(settings);
            });

            services.AddScoped<ISearchService, NoOpSearchService>();
            services.AddScoped<IElasticBulkService, NoOpElasticBulkService>();
            services.AddScoped<IElasticIndexManager, NoOpElasticIndexManager>();
            services.AddScoped<ISearchDatabaseSyncService, NoOpSearchDatabaseSyncService>();
            services.AddScoped<ISearchStatsService, NoOpSearchStatsService>();
        }
    }

    private static void AddBackgroundServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<OutboxProcessorBackgroundService>();
        services.AddHostedService<ExpiredSessionCleanupJob>();
        services.AddHostedService<ExpiredOrderCleanupJob>();
        services.AddHostedService<WalletReservationExpiryJob>();
        services.AddHostedService<WalletReconciliationJob>();
        services.AddHostedService<InventoryReservationExpiryService>();
        services.AddHostedService<OrphanedFileCleanupService>();
        services.AddHostedService<PaymentCleanupService>();

        var elasticOptions = configuration.GetSection(ElasticsearchOptions.SectionName).Get<ElasticsearchOptions>()
            ?? new ElasticsearchOptions();

        if (elasticOptions.IsEnabled && elasticOptions.EnableBackgroundSync)
        {
            services.AddHostedService<ElasticsearchSyncBackgroundService>();
            services.AddHostedService<ElasticsearchOutboxProcessor>();
        }
    }

    private static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<DBContext>("database")
            .AddRedis(
                configuration.GetConnectionString("Redis") ?? "localhost:6379",
                name: "redis");
    }

    private static void AddDataProtectionWithRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(
                () => ConnectionMultiplexer.Connect(redisConnectionString).GetDatabase(),
                "DataProtection-Keys")
            .SetApplicationName(configuration["App:Name"] ?? "ShopApp");
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
    }
}