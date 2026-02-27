using Infrastructure.Cache.Health;
using Infrastructure.Cache.Options;
using Infrastructure.Search.HealthChecks;
using Infrastructure.Search.Options;
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
        AddAuthServices(services);
        AddRepositories(services);
        AddDomainAndApplicationServices(services, configuration);
        AddPaymentServices(services, configuration);
        AddWalletServices(services);
        AddBackgroundServices(services, configuration);
        AddEventHandlers(services);
        AddCachingAndConcurrency(services, configuration);
        AddRedisServices(services, configuration);
        AddElasticsearchServices(services, configuration);
        AddHealthChecks(services, connectionString);

        return services;
    }

    private static void AddDatabaseServices(
        IServiceCollection services,
        string connectionString)
    {
        services.AddScoped<AuditableEntityInterceptor>();

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

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<DBContext>());

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

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<Application.Order.Sagas.OrderProcessManagerSaga>());
    }

    private static void AddAuthServices(IServiceCollection services)
    {
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
        services.AddScoped<IWalletRepository, Infrastructure.Wallet.Repositories.WalletRepository>();
        services.AddScoped<IShippingRepository, ShippingMethodRepository>();
        services.AddScoped<IVariantRepository, VariantRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderStatusRepository, OrderStatusRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IOrderProcessStateRepository, OrderProcessStateRepository>();
    }

    private static void AddDomainAndApplicationServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheOptions>(
            configuration.GetSection(CacheOptions.SectionName));

        var cacheOptions = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<InventoryDomainService>();
        services.AddScoped<IStockLedgerService, StockLedgerService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IStorageService, LiaraStorageService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IReviewQueryService, ReviewQueryService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IShippingQueryService, ShippingQueryService>();
        services.AddScoped<IMediaQueryService, MediaQueryService>();
        services.AddScoped<ICartQueryService, CartQueryService>();
        services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
        services.AddScoped<IDiscountService, DiscountService>();
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

    private static void AddPaymentServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PaymentGatewayOptions>(
            configuration.GetSection(PaymentGatewayOptions.SectionName));

        services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();
        services.AddScoped<IPaymentService, IdempotentPaymentService>();

        services.AddHttpClient<IPaymentGateway, ZarinPalPaymentGateway>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
    }

    private static void AddWalletServices(IServiceCollection services)
    {
        services.AddScoped<INotificationHandler<PaymentSucceededEvent>,
            Application.Wallet.EventHandlers.PaymentSucceededWalletCreditEventHandler>();
        services.AddScoped<INotificationHandler<PaymentRefundedEvent>,
            Application.Wallet.EventHandlers.PaymentRefundedWalletEventHandler>();
        services.AddScoped<INotificationHandler<OrderCancelledEvent>,
            Application.Wallet.EventHandlers.OrderCancelledWalletReleaseEventHandler>();
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
        services.AddScoped<INotificationHandler<VariantStockChangedApplicationNotification>,
            VariantStockCacheInvalidationHandler>();
        services.AddScoped<INotificationHandler<VariantStockChangedEvent>,
            InventoryStockSearchSyncHandler>();
        services.AddScoped<INotificationHandler<StockCommittedEvent>,
            InventoryStockSearchSyncHandler>();
        services.AddScoped<INotificationHandler<StockReturnedEvent>,
            InventoryStockSearchSyncHandler>();
        services.AddScoped<INotificationHandler<PaymentSucceededEvent>,
            PaymentSucceededInventoryCommitEventHandler>();
    }

    private static void AddCachingAndConcurrency(
            IServiceCollection services,
            IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        if (cacheOptions.IsEnabled)
        {
            services.AddSingleton<IDistributedLock, RedisDistributedLock>();
        }
        else
        {
            services.AddSingleton<IDistributedLock, NoOpDistributedLock>();
        }
    }

    private static void AddRedisServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        var redisConn = configuration.GetConnectionString("Redis");

        if (cacheOptions.IsEnabled && !string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConn));
        }
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
                var logger = sp.GetRequiredService<ILogger<ElasticSearchService>>();
                return ElasticClientFactory.Create(configuration, logger);
            });

            services.AddSingleton<ElasticsearchMetrics>();
            services.AddSingleton<ElasticsearchCircuitBreaker>();
            services.AddScoped<ElasticSearchService>();
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
    string connectionString)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql", "postgresql" });

        services.AddHealthChecks()
            .AddCheck<ElasticsearchHealthCheck>(
                "elasticsearch",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "search", "elasticsearch" });

        services.AddHealthChecks()
            .AddCheck<RedisCacheHealthCheck>(
                "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "cache", "redis" });
    }
}