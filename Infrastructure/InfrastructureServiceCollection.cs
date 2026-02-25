using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Infrastructure;

public static class InfrastructureServiceCollection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PoolerConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        AddDatabaseServices(services, connectionString);
        AddCoreInfrastructure(services);
        AddAuthServices(services);
        AddRepositories(services);
        AddDomainAndApplicationServices(services);
        AddPaymentServices(services, configuration);
        AddWalletServices(services);
        AddBackgroundServices(services);
        AddEventHandlers(services);
        AddCachingAndConcurrency(services);
        AddRedisServices(services, configuration);
        AddHealthChecks(services, connectionString);

        return services;
    }

    private static void AddDatabaseServices(IServiceCollection services, string connectionString)
    {
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddDbContext<DbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
                   .AddInterceptors(interceptor);
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
    }

    private static void AddCoreInfrastructure(IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<IUrlResolverService, UrlResolverService>();
        services.AddSingleton<IAuditMaskingService, AuditMaskingService>();
        services.AddScoped<IAuditService, EnhancedAuditService>();
        services.AddScoped<ISearchDatabaseSyncService, ElasticsearchDatabaseSyncService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<Application.Order.Sagas.OrderProcessManagerSaga>());
    }

    private static void AddAuthServices(IServiceCollection services)
    {
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
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
    }

    private static void AddDomainAndApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<InventoryDomainService>();
        services.AddScoped<IStockLedgerService, StockLedgerService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddMemoryCache();
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

    private static void AddBackgroundServices(IServiceCollection services)
    {
        services.AddHostedService<InventoryReservationExpiryService>();
        services.AddHostedService<ElasticsearchOutboxProcessor>();
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

    private static void AddCachingAndConcurrency(IServiceCollection services)
    {
        services.AddSingleton<IDistributedLock, RedisDistributedLock>();
    }

    private static void AddRedisServices(IServiceCollection services, IConfiguration configuration)
    {
        var redisConn = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConn))
            return;

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConn));
    }

    private static void AddHealthChecks(IServiceCollection services, string connectionString)
    {
        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql")
            .AddCheck<ElasticsearchDLQHealthCheck>("elasticsearch_dlq")
            .AddCheck<RedisCacheHealthCheck>(
                "redis-cache",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", "infrastructure"]);
    }
}