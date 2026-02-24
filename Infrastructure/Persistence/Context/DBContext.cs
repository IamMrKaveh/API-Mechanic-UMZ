namespace Infrastructure.Persistence.Context;

public class DBContext : DbContext, IApplicationDbContext
{
    public DBContext(DbContextOptions<DBContext> options) : base(options)
    {
    }

    public DbSet<Domain.User.User> Users => Set<Domain.User.User>();

    public DbSet<UserOtp> UserOtps => Set<UserOtp>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Domain.Product.Product> Products => Set<Domain.Product.Product>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<AttributeType> AttributeTypes => Set<AttributeType>();
    public DbSet<Domain.Order.Order> Orders => Set<Domain.Order.Order>();
    public DbSet<Domain.Shipping.Shipping> Shippings => Set<Domain.Shipping.Shipping>();
    public DbSet<Domain.Cart.Cart> Carts => Set<Domain.Cart.Cart>();
    public DbSet<Domain.Category.Category> Categories => Set<Domain.Category.Category>();
    public DbSet<Domain.Brand.Brand> Brands => Set<Domain.Brand.Brand>();
    public DbSet<Domain.Media.Media> Medias => Set<Domain.Media.Media>();
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

    public DbSet<Domain.Wallet.Wallet> Wallets => Set<Domain.Wallet.Wallet>();
    public DbSet<WalletLedgerEntry> WalletLedgerEntries => Set<WalletLedgerEntry>();
    public DbSet<WalletReservation> WalletReservations => Set<WalletReservation>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Money>()
            .HaveConversion<MoneyConverter>();
    }
}