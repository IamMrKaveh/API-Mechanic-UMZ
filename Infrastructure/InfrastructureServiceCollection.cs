using Application.Analytics.Contracts;
using Application.Audit.Contracts;
using Application.Auth.Contracts;
using Application.Brand.Contracts;
using Application.Cache.Contracts;
using Application.Cart.Contracts;
using Application.Category.Contracts;
using Application.Common.Contracts;
using Application.Common.Interfaces;
using Application.Communication.Contracts;
using Application.Discount.Contracts;
using Application.Inventory.Contracts;
using Application.Location.Contracts;
using Application.Media.Contracts;
using Application.Notification.Contracts;
using Application.Order.Contracts;
using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Payment.Contracts;
using Application.Product.Contracts;
using Application.Review.Contracts;
using Application.Search.Contracts;
using Application.Security.Contracts;
using Application.Security.Interfaces;
using Application.Shipping.Contracts;
using Application.Support.Contracts;
using Application.User.Contracts;
using Application.Wallet.Contracts;
using Application.Wallet.EventHandlers;
using Domain.Attribute.Interfaces;
using Domain.Audit.Interfaces;
using Domain.Cart.Interfaces;
using Domain.Category.Interfaces;
using Domain.Common.Interfaces;
using Domain.Discount.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Media.Interfaces;
using Domain.Notification.Interfaces;
using Domain.Order.Events;
using Domain.Order.Interfaces;
using Domain.Payment.Events;
using Domain.Payment.Interfaces;
using Domain.Product.Interfaces;
using Domain.Review.Interfaces;
using Domain.Security.Interfaces;
using Domain.Shipping.Interfaces;
using Domain.Support.Interfaces;
using Domain.User.Interfaces;
using Domain.Variant.Interfaces;
using Domain.Wallet.Interfaces;
using Ganss.Xss;
using Infrastructure.Analytics.Services;
using Infrastructure.Attribute.Repositories;
using Infrastructure.Audit.BackgroundServices;
using Infrastructure.Audit.QueryServices;
using Infrastructure.Audit.Repositories;
using Infrastructure.Audit.Services;
using Infrastructure.Auth.Options;
using Infrastructure.Auth.Repositories;
using Infrastructure.Auth.Services;
using Infrastructure.Brand.QueryServices;
using Infrastructure.Cache.Options;
using Infrastructure.Cache.Redis.Services;
using Infrastructure.Cache.Services;
using Infrastructure.Cart.QueryServices;
using Infrastructure.Cart.Repositories;
using Infrastructure.Category.QueryServices;
using Infrastructure.Category.Repositories;
using Infrastructure.Common.Services;
using Infrastructure.Communication.Options;
using Infrastructure.Communication.Services;
using Infrastructure.Discount.Repositories;
using Infrastructure.Discount.Services;
using Infrastructure.Inventory.BackgroundServices;
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
using Infrastructure.Order.BackgroundServices;
using Infrastructure.Order.QueryServices;
using Infrastructure.Order.Repositories;
using Infrastructure.Order.Services;
using Infrastructure.Payment.BackgroundServices;
using Infrastructure.Payment.Factory;
using Infrastructure.Payment.QueryServices;
using Infrastructure.Payment.Repositories;
using Infrastructure.Payment.Services;
using Infrastructure.Payment.ZarinPal;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Product.QueryServices;
using Infrastructure.Product.Repositories;
using Infrastructure.Review.QueryServices;
using Infrastructure.Review.Repositories;
using Infrastructure.Search;
using Infrastructure.Search.BackgroundServices;
using Infrastructure.Search.HealthChecks;
using Infrastructure.Search.Options;
using Infrastructure.Search.Services;
using Infrastructure.Security.Services;
using Infrastructure.Security.Tools;
using Infrastructure.Shipping.QueryServices;
using Infrastructure.Shipping.Repositories;
using Infrastructure.Storage.Services;
using Infrastructure.Support.QueryServices;
using Infrastructure.Support.Repositories;
using Infrastructure.User.QueryServices;
using Infrastructure.User.Repositories;
using Infrastructure.Variant.Repositories;
using Infrastructure.Wallet.QueryServices;
using Infrastructure.Wallet.Repositories;
using Infrastructure.Wallet.Services;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Infrastructure;

public static class InfrastructureServiceCollection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("PoolerConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        AddDatabaseServices(services, connectionString);
        AddCoreInfrastructure(services);
        AddAuthServices(services, configuration);
        AddRepositories(services);
        AddDomainAndApplicationServices(services, configuration);
        RegisterCheckoutServices(services);
        AddPaymentServices(services, configuration);
        AddWalletServices(services);
        AddBackgroundServices(services, configuration);
        AddEventHandlers(services);
        AddCachingAndConcurrency(services, configuration);
        AddElasticsearchServices(services, configuration);
        AddHealthChecks(services, configuration, connectionString);

        services.AddHttpClient<ILocationService, LocationService>(client =>
        {
            client.BaseAddress = new Uri("https://iran-locations-api.ir/api/v1/fa/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }

    private static void AddDatabaseServices(
        IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<DBContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();

            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.EnableRetryOnFailure(0);
                });

            options.AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
    }

    private static void AddCoreInfrastructure(IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<IUrlResolverService, UrlResolverService>();
        services.AddSingleton<IAuditMaskingService, AuditMaskingService>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IAuditService, EnhancedAuditService>();
        services.AddTransient<IHtmlSanitizer, HtmlSanitizer>();
    }

    private static void AddAuthServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmtpOptions>(
            configuration.GetSection(SmtpOptions.SectionName));

        services.Configure<KavenegarOptions>(
            configuration.GetSection(KavenegarOptions.SectionName));

        services.AddScoped<ISessionRepository, SessionRepository>();
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRateLimitService, RateLimitService>();
        services.AddScoped<IEmailService, EmailService>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IShippingRepository, ShippingRepository>();
        services.AddScoped<IVariantRepository, VariantRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderStatusRepository, OrderStatusRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IOrderProcessStateRepository, OrderProcessStateRepository>();
    }

    private static void AddDomainAndApplicationServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.Configure<CacheOptions>(
            configuration.GetSection(CacheOptions.SectionName));

        var cacheOptions = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<InventoryDomainService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IStockLedgerService, StockLedgerService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IStorageService, LiaraStorageService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IOrderStatusQueryService, OrderStatusQueryService>();
        services.AddScoped<IPaymentQueryService, PaymentQueryService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IReviewQueryService, ReviewQueryService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IShippingQueryService, ShippingQueryService>();
        services.AddScoped<IMediaQueryService, MediaQueryService>();
        services.AddScoped<ICartQueryService, CartQueryService>();
        services.AddScoped<ISupportQueryService, TicketQueryService>();
        services.AddScoped<IWalletQueryService, WalletQueryService>();
        services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<INotificationQueryService, NotificationQueryService>();
        services.AddScoped<IBrandQueryService, BrandQueryService>();
        services.AddScoped<CartItemValidationService>();

        services.AddMemoryCache();
        if (cacheOptions.IsEnabled)
        {
            var redisConn = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                services.AddScoped<ICacheService, RedisCacheService>();
            }
            else
            {
                services.AddScoped<ICacheService, InMemoryCacheService>();
            }
        }
        else
        {
            services.AddScoped<ICacheService, InMemoryCacheService>();
        }
    }

    private static void RegisterCheckoutServices(IServiceCollection services)
    {
        services.AddScoped<ICheckoutAddressResolverService, CheckoutAddressResolverService>();
        services.AddScoped<ICheckoutShippingValidatorService, CheckoutShippingValidatorService>();
        services.AddScoped<ICheckoutCartItemBuilderService, CheckoutCartItemBuilderService>();
        services.AddScoped<ICheckoutPriceValidatorService, CheckoutPriceValidatorService>();
        services.AddScoped<ICheckoutStockValidatorService, CheckoutStockValidatorService>();
        services.AddScoped<ICheckoutOrderCreationService, CheckoutOrderCreationService>();
        services.AddScoped<ICheckoutDiscountApplicatorService, CheckoutDiscountApplicatorService>();
        services.AddScoped<ICheckoutPaymentProcessorService, CheckoutPaymentProcessorService>();
        services.AddScoped<ICheckoutOrchestrationService, CheckoutOrchestrationService>();
    }

    private static void AddPaymentServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaymentGatewayOptions>(
            configuration.GetSection(PaymentGatewayOptions.SectionName));

        services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddHttpClient<IPaymentGateway, ZarinPalPaymentGateway>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
    }

    private static void AddWalletServices(IServiceCollection services)
    {
        services.AddScoped<INotificationHandler<PaymentSucceededEvent>, PaymentSucceededWalletCreditEventHandler>();
        services.AddScoped<INotificationHandler<PaymentRefundedEvent>, PaymentRefundedWalletEventHandler>();
        services.AddScoped<INotificationHandler<OrderCancelledEvent>, OrderCancelledWalletReleaseEventHandler>();
    }

    private static void AddBackgroundServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var elasticOptions = configuration.GetSection(ElasticsearchOptions.SectionName)
            .Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();

        if (elasticOptions.IsEnabled && elasticOptions.EnableBackgroundSync)
        {
            services.AddHostedService<ElasticsearchOutboxProcessor>();
            services.AddHostedService<ElasticsearchSyncBackgroundService>();
        }

        services.AddHostedService<InventoryReservationExpiryService>();
        services.AddHostedService<PaymentCleanupService>();
        services.AddHostedService<PaymentReconciliationService>();
        services.AddHostedService<OrderExpiryBackgroundService>();
        services.AddHostedService<AuditRetentionService>();
    }

    private static void AddEventHandlers(IServiceCollection services)
    {
    }

    private static void AddCachingAndConcurrency(
        IServiceCollection services,
        IConfiguration configuration)
    {
    }

    private static void AddElasticsearchServices(
     IServiceCollection services,
     IConfiguration configuration)
    {
        services.Configure<ElasticsearchOptions>(
            configuration.GetSection(ElasticsearchOptions.SectionName));

        var elasticOptions = configuration.GetSection(ElasticsearchOptions.SectionName)
            .Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();

        if (elasticOptions.IsEnabled)
        {
            services.AddSingleton<ElasticsearchClient>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ElasticsearchService>>();
                return ElasticClientFactory.Create(configuration, logger);
            });

            services.AddSingleton<ElasticsearchMetrics>();
            services.AddSingleton<ElasticsearchCircuitBreaker>();
            services.AddScoped<ElasticsearchService>();
            services.AddScoped<ISearchService, ResilientElasticSearchService>();
            services.AddScoped<IElasticIndexManager, ElasticIndexManager>();
            services.AddScoped<IElasticBulkService, ElasticBulkService>();
            services.AddScoped<ISearchDatabaseSyncService, ElasticsearchDatabaseSyncService>();
            services.AddScoped<ISearchStatsService, ElasticsearchStatsService>();
        }
        else
        {
            services.AddScoped<ISearchService, NoOpSearchService>();
            services.AddScoped<IElasticIndexManager, NoOpElasticIndexManager>();
            services.AddScoped<IElasticBulkService, NoOpElasticBulkService>();
            services.AddScoped<ISearchDatabaseSyncService, NoOpSearchDatabaseSyncService>();
            services.AddScoped<ISearchStatsService, NoOpSearchStatsService>();
        }
    }

    private static void AddHealthChecks(
        IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        var healthChecksBuilder = services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "sql", "postgresql"]);

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["cache", "redis"]);
        }

        healthChecksBuilder.AddCheck<ElasticsearchHealthCheck>(
            "elasticsearch",
            failureStatus: HealthStatus.Degraded,
            tags: ["search", "elasticsearch"]);
    }
}