using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Infrastructure;

public static class InfrastructureServiceCollection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // -----------------------------
        // DB Context
        // -----------------------------
        string connectionString = configuration.GetConnectionString("PoolerConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        services.AddDbContext<DbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
                   .AddInterceptors(interceptor);
        });

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql");

        // -----------------------------
        // Unit of Work & Domain Events
        // -----------------------------
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IUrlResolverService, UrlResolverService>();
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();

        // -----------------------------
        // Auth & Session
        // -----------------------------
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();

        // -----------------------------
        // Repositories
        // -----------------------------
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

        // ─── Core Inventory Services ─────────────────────────────────────────
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<InventoryDomainService>();

        // ─── BackgroundService برای آزادسازی رزروهای منقضی ──────────
        services.AddHostedService<InventoryReservationExpiryService>();

        // ─── Cache Invalidation Handler ──────────────────────────────
        services.AddScoped<INotificationHandler<VariantStockChangedApplicationNotification>, VariantStockCacheInvalidationHandler>();

        // ─── Search Sync Handler ─────────────────────────────────────
        services.AddScoped<INotificationHandler<VariantStockChangedEvent>, InventoryStockSearchSyncHandler>();
        services.AddScoped<INotificationHandler<StockCommittedEvent>, InventoryStockSearchSyncHandler>();
        services.AddScoped<INotificationHandler<StockReturnedEvent>, InventoryStockSearchSyncHandler>();

        // ─── Payment Succeeded Event Handler ─────────────────────────
        services.AddScoped<INotificationHandler<PaymentSucceededEvent>, PaymentSucceededInventoryCommitEventHandler>();

        // -----------------------------
        // Background Services
        // -----------------------------
        services.AddHostedService<ElasticsearchOutboxProcessor>();

        services.AddHealthChecks()
            .AddCheck<ElasticsearchDLQHealthCheck>("elasticsearch_dlq");

        services.AddHostedService<PaymentCleanupService>();

        // -----------------------------
        // Infrastructure
        // -----------------------------
        services.AddScoped<ISearchDatabaseSyncService, ElasticsearchDatabaseSyncService>();
        services.AddSingleton<AuditableEntityInterceptor>();

        // Saga
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<
            Application.Order.Sagas.OrderProcessManagerSaga>());

        // Background Services
        services.AddHostedService<OrderExpiryBackgroundService>();

        // تنظیمات درگاه پرداخت
        services.Configure<PaymentGatewayOptions>(
            configuration.GetSection(PaymentGatewayOptions.SectionName));

        services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();
        services.AddScoped<IPaymentService, IdempotentPaymentService>();
        services.AddScoped<IMediaService, MediaService>();

        services.AddHttpClient<IPaymentGateway, ZarinPalPaymentGateway>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Background Services
        services.AddHostedService<PaymentReconciliationService>();

        services.AddScoped<IStockLedgerService, StockLedgerService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        services.AddSingleton<IDistributedLock, RedisDistributedLock>();
        services.AddSingleton<IAuditMaskingService, AuditMaskingService>();
        services.AddScoped<IAuditService, EnhancedAuditService>();
        services.AddHostedService<AuditRetentionService>();

        services.AddMemoryCache();

        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConn));

            // Health Checks
            services.AddHealthChecks()
                .AddCheck<RedisCacheHealthCheck>(
                    "redis-cache",
                    failureStatus: HealthStatus.Degraded,
                    tags: ["cache", "redis", "infrastructure"]);
        }
        return services;
    }
}