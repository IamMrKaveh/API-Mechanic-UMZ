using Infrastructure.Audit;

namespace Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IShippingMethodRepository, ShippingMethodRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryGroupRepository, CategoryGroupRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        services.AddScoped<IOrderStatusRepository, OrderStatusRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();

        services.AddHostedService<PaymentCleanupService>();
        services.AddHostedService<PaymentVerificationJob>();
        services.AddHostedService<OrphanedFileCleanupService>();

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAdminAnalyticsService, AnalyticsService>();
        services.AddSingleton<IStorageService, LiaraStorageService>();
        services.AddSingleton<IRateLimitService, RateLimitService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILogger<ElasticsearchClient>>();
            return ElasticClientFactory.Create(config, logger);
        });

        services.AddScoped<ElasticSearchService>();
        services.AddScoped<ISearchService, ResilientElasticSearchService>();
        services.AddScoped<IElasticBulkService, ElasticBulkService>();
        services.AddScoped<IElasticIndexManager, ElasticIndexManager>();
        services.AddScoped<IElasticDeadLetterQueue, ElasticDeadLetterQueue>();
        services.AddSingleton<ElasticsearchCircuitBreaker>();
        services.AddHostedService<ElasticsearchSyncBackgroundService>();
        services.AddHostedService<DeadLetterQueueProcessor>();
        services.AddSingleton<ElasticsearchMetrics>();

        services.AddScoped<ElasticsearchInitialSyncService>();
        services.AddScoped<ElasticsearchDatabaseSyncService>();

        return services;
    }
}