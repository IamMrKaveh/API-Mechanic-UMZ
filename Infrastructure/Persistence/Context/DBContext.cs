using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Domain.Audit.Entities;
using Domain.Inventory.Aggregates;
using Domain.Inventory.Entities;
using Domain.Payment.Aggregates;
using Domain.Review.Aggregates;
using Domain.Support.Aggregates;
using Domain.User.Entities;
using Domain.Variant.Aggregates;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Entities;

namespace Infrastructure.Persistence.Context;

public sealed class DBContext : DbContext, IApplicationDbContext
{
    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
        ChangeTracker.AutoDetectChangesEnabled = false;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
    }

    public DbSet<Domain.User.Aggregates.User> Users => Set<Domain.User.Aggregates.User>();
    public DbSet<UserOtp> UserOtps => Set<UserOtp>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Domain.Wishlist.Aggregates.Wishlist> Wishlists => Set<Domain.Wishlist.Aggregates.Wishlist>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Domain.Product.Product> Products => Set<Domain.Product.Product>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<AttributeType> AttributeTypes => Set<AttributeType>();
    public DbSet<Domain.Order.Aggregates.Order> Orders => Set<Domain.Order.Aggregates.Order>();
    public DbSet<Domain.Shipping.Aggregates.Shipping> Shippings => Set<Domain.Shipping.Aggregates.Shipping>();
    public DbSet<Domain.Cart.Aggregates.Cart> Carts => Set<Domain.Cart.Aggregates.Cart>();
    public DbSet<Domain.Category.Aggregates.Category> Categories => Set<Domain.Category.Aggregates.Category>();
    public DbSet<Domain.Brand.Aggregates.Brand> Brands => Set<Domain.Brand.Aggregates.Brand>();
    public DbSet<Domain.Media.Aggregates.Media> Medias => Set<Domain.Media.Aggregates.Media>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Domain.Notification.Notification> Notifications => Set<Domain.Notification.Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<RateLimitEntry> RateLimits => Set<RateLimitEntry>();
    public DbSet<FailedElasticOperation> FailedElasticOperations => Set<FailedElasticOperation>();
    public DbSet<ElasticsearchOutboxMessage> ElasticsearchOutboxMessages => Set<ElasticsearchOutboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<AttributeValue> AttributeValues => Set<AttributeValue>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<Domain.Wallet.Aggregates.Wallet> Wallets => Set<Domain.Wallet.Aggregates.Wallet>();
    public DbSet<WalletLedgerEntry> WalletLedgerEntries => Set<WalletLedgerEntry>();
    public DbSet<WalletReservation> WalletReservations => Set<WalletReservation>();
    public DbSet<WalletReconciliationAudit> WalletReconciliationAudits => Set<WalletReconciliationAudit>();
    public DbSet<OrderProcessState> OrderProcessStates => Set<OrderProcessState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Money>()
            .HaveConversion<MoneyConverter>();
    }
}