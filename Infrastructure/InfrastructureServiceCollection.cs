using Infrastructure.Audit.BackgroundServices;
using Infrastructure.Audit.Services;
using Infrastructure.Cache.Redis.Lock;
using Infrastructure.Cache.Services;
using Infrastructure.Order.BackgroundServices;
using Infrastructure.Payment.Factory;
using Infrastructure.Payment.Services;
using IConfigurationProvider = AutoMapper.IConfigurationProvider;

namespace Infrastructure;

public static class InfrastructureServiceCollection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // LoggerFactory required by AutoMapper 16
        var loggerFactory = new NullLoggerFactory();

        var mapperConfig = new MapperConfiguration(
            cfg =>
            {
                cfg.AllowNullCollections = true;
                cfg.AllowNullDestinationValues = true;

                // Register mapping profiles
                cfg.AddMaps(typeof(MappingProfile).Assembly);
            },
            loggerFactory
        );

        mapperConfig.AssertConfigurationIsValid();

        services.AddSingleton<IConfigurationProvider>(mapperConfig);
        services.AddSingleton<IMapper>(sp =>
            mapperConfig.CreateMapper(sp.GetService));

        // -----------------------------
        // Unit of Work & Domain Events
        // -----------------------------
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // -----------------------------
        // Auth & Session
        // -----------------------------
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

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

        // -----------------------------
        // Query Services
        // -----------------------------
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<ICartQueryService, CartQueryService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IMediaQueryService, MediaQueryService>();
        services.AddScoped<IReviewQueryService, ReviewQueryService>();
        services.AddScoped<INotificationService, NotificationService>();

        // ─── Core Inventory Services ─────────────────────────────────────────
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<InventoryDomainService>();

        // ─── FIX #1: BackgroundService برای آزادسازی رزروهای منقضی ──────────
        services.AddHostedService<InventoryReservationExpiryService>();

        // ─── FIX #7: Cache Invalidation Handler ──────────────────────────────
        // VariantStockCacheInvalidationHandler از طریق MediatR Pipeline register می‌شود
        // (چون INotificationHandler است - اگر از MediatR DI scan استفاده می‌کنید خودکار register می‌شود)
        // در غیر این صورت:
        services.AddScoped<INotificationHandler<VariantStockChangedEvent>, VariantStockCacheInvalidationHandler>();

        // ─── FIX #8: Search Sync Handler ─────────────────────────────────────
        services.AddScoped<INotificationHandler<VariantStockChangedEvent>, InventoryStockSearchSyncHandler>();
        services.AddScoped<INotificationHandler<StockCommittedEvent>, InventoryStockSearchSyncHandler>();
        services.AddScoped<INotificationHandler<StockReturnedEvent>, InventoryStockSearchSyncHandler>();

        // ─── FIX #2: Payment Succeeded Event Handler ─────────────────────────
        services.AddScoped<INotificationHandler<PaymentSucceededEvent>, PaymentSucceededInventoryCommitEventHandler>();

        // -----------------------------
        // Background Services
        // -----------------------------
        services.AddHostedService<ElasticsearchOutboxProcessor>();
        services.AddHostedService<PaymentCleanupService>();

        // -----------------------------
        // Infrastructure
        // -----------------------------
        services.AddScoped<ISearchDatabaseSyncService, ElasticsearchDatabaseSyncService>();

        // Saga
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<
            Application.Order.Sagas.OrderProcessManagerSaga>());

        // Background Services
        services.AddHostedService<OrderExpiryBackgroundService>();

        // تنظیمات درگاه پرداخت
        services.Configure<PaymentGatewayOptions>(
            configuration.GetSection(PaymentGatewayOptions.SectionName));

        // Factory - تمام IPaymentGateway‌ها به صورت خودکار تزریق می‌شوند
        services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();

        // Idempotent Payment Service جایگزین سرویس قبلی
        services.AddScoped<IPaymentService, IdempotentPaymentService>();

        // Background Services
        services.AddHostedService<PaymentReconciliationService>();

        services.AddScoped<IStockLedgerService, StockLedgerService>();

        // Cache Invalidation Service
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();

        // Distributed Lock
        services.AddSingleton<IDistributedLock, RedisDistributedLock>();

        // Masking Service
        services.AddSingleton<IAuditMaskingService, AuditMaskingService>();

        // Enhanced Audit Service (جایگزین AuditService قبلی)
        services.AddScoped<IAuditService, EnhancedAuditService>();

        // Retention Background Service
        services.AddHostedService<AuditRetentionService>();

        services.AddMemoryCache();

        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConn));
        }

        services.AddHealthChecks();

        return services;
    }
}