using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Infrastructure;

public static class InfrastructureServiceCollection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        
        
        
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

        
        
        
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IUrlResolverService, UrlResolverService>();
        services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();

        
        
        
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();

        
        
        
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

        
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<InventoryDomainService>();

        
        services.AddHostedService<InventoryReservationExpiryService>();

        
        services.AddScoped<INotificationHandler<VariantStockChangedApplicationNotification>, VariantStockCacheInvalidationHandler>();

        
        services.AddScoped<INotificationHandler<VariantStockChangedEvent>, InventoryStockSearchSyncHandler>();
        services.AddScoped<INotificationHandler<StockCommittedEvent>, InventoryStockSearchSyncHandler>();
        services.AddScoped<INotificationHandler<StockReturnedEvent>, InventoryStockSearchSyncHandler>();

        
        services.AddScoped<INotificationHandler<PaymentSucceededEvent>, PaymentSucceededInventoryCommitEventHandler>();

        
        
        
        services.AddHostedService<ElasticsearchOutboxProcessor>();

        services.AddHealthChecks()
            .AddCheck<ElasticsearchDLQHealthCheck>("elasticsearch_dlq");

        services.AddHostedService<PaymentCleanupService>();

        
        
        
        services.AddScoped<ISearchDatabaseSyncService, ElasticsearchDatabaseSyncService>();
        services.AddSingleton<AuditableEntityInterceptor>();

        
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<
            Application.Order.Sagas.OrderProcessManagerSaga>());

        
        services.AddHostedService<OrderExpiryBackgroundService>();

        
        services.Configure<PaymentGatewayOptions>(
            configuration.GetSection(PaymentGatewayOptions.SectionName));

        services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();
        services.AddScoped<IPaymentService, IdempotentPaymentService>();
        services.AddScoped<IMediaService, MediaService>();

        services.AddHttpClient<IPaymentGateway, ZarinPalPaymentGateway>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        
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

            
            services.AddHealthChecks()
                .AddCheck<RedisCacheHealthCheck>(
                    "redis-cache",
                    failureStatus: HealthStatus.Degraded,
                    tags: ["cache", "redis", "infrastructure"]);
        }
        return services;
    }
}