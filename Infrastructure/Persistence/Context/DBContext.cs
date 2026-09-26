using System.Reflection;
using Application.Order.Sagas.State;
using Application.Search.Features.Shared;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Domain.Audit.Entities;
using Domain.Brand.ValueObjects;
using Domain.Cart.Entities;
using Domain.Category.ValueObjects;
using Domain.Discount.Aggregates;
using Domain.Inventory.Aggregates;
using Domain.Inventory.Entities;
using Domain.Order.Entities;
using Domain.Payment.Aggregates;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.Entities;
using Domain.Security.Aggregates;
using Domain.Support.Aggregates;
using Domain.Support.Entities;
using Domain.User.Entities;
using Domain.Variant.Aggregates;
using Domain.Variant.Entities;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Entities;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Infrastructure.Search;

namespace Infrastructure.Persistence.Context;

public sealed class DBContext(
    DbContextOptions<DBContext> options,
    AuditableEntityInterceptor auditableInterceptor,
    DomainEventInterceptor domainEventInterceptor) : DbContext(options)
{
    public DbSet<AttributeType> AttributeTypes => Set<AttributeType>();
    public DbSet<AttributeValue> AttributeValues => Set<AttributeValue>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Domain.Brand.Aggregates.Brand> Brands => Set<Domain.Brand.Aggregates.Brand>();
    public DbSet<Domain.Cart.Aggregates.Cart> Carts => Set<Domain.Cart.Aggregates.Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Domain.Category.Aggregates.Category> Categories => Set<Domain.Category.Aggregates.Category>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<ElasticsearchOutboxMessage> ElasticsearchOutboxMessages => Set<ElasticsearchOutboxMessage>();
    public DbSet<FailedElasticOperation> FailedElasticOperations => Set<FailedElasticOperation>();
    public DbSet<Domain.Inventory.Aggregates.Inventory> Inventories => Set<Domain.Inventory.Aggregates.Inventory>();
    public DbSet<Domain.Media.Aggregates.Media> Medias => Set<Domain.Media.Aggregates.Media>();
    public DbSet<Domain.Notification.Aggregates.Notification> Notifications => Set<Domain.Notification.Aggregates.Notification>();
    public DbSet<Domain.Order.Aggregates.Order> Orders => Set<Domain.Order.Aggregates.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<OrderProcessState> OrderProcessStates => Set<OrderProcessState>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OutboxArchiveMessage> OutboxArchiveMessages => Set<OutboxArchiveMessage>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Domain.Product.Aggregates.Product> Products => Set<Domain.Product.Aggregates.Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ReviewVote> ReviewVotes => Set<ReviewVote>();
    public DbSet<RateLimitEntry> RateLimitEntries => Set<RateLimitEntry>();
    public DbSet<Domain.Shipping.Aggregates.Shipping> Shippings => Set<Domain.Shipping.Aggregates.Shipping>();
    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<Domain.User.Aggregates.User> Users => Set<Domain.User.Aggregates.User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<UserOtp> UserOtps => Set<UserOtp>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<VariantAttribute> VariantAttributes => Set<VariantAttribute>();
    public DbSet<VariantShipping> VariantShippings => Set<VariantShipping>();
    public DbSet<Domain.Wallet.Aggregates.Wallet> Wallets => Set<Domain.Wallet.Aggregates.Wallet>();
    public DbSet<WalletFraudAlert> WalletFraudAlerts => Set<WalletFraudAlert>();
    public DbSet<WalletLedgerEntry> WalletLedgerEntries => Set<WalletLedgerEntry>();
    public DbSet<WalletReservation> WalletReservations => Set<WalletReservation>();
    public DbSet<WalletTopUp> WalletTopUps => Set<WalletTopUp>();
    public DbSet<WalletWithdrawalRequest> WalletWithdrawalRequests => Set<WalletWithdrawalRequest>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Domain.Wishlist.Aggregates.Wishlist> Wishlists => Set<Domain.Wishlist.Aggregates.Wishlist>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ConfigureStronglyTypedIds(configurationBuilder);

        configurationBuilder.Properties<decimal>()
            .HavePrecision(18, 4);

        base.ConfigureConventions(configurationBuilder);
    }

    private static void ConfigureStronglyTypedIds(ModelConfigurationBuilder configurationBuilder)
    {
        var converterOpenType = typeof(StronglyTypedIdConverter<>);
        var configureMethod = typeof(DBContext).GetMethod(
            nameof(ConfigureStronglyTypedId),
            BindingFlags.Static | BindingFlags.NonPublic)!;

        var idTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch
                {
                    return Array.Empty<Type>();
                }
            })
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IStronglyTypedId).IsAssignableFrom(t));

        foreach (var idType in idTypes)
        {
            configureMethod
                .MakeGenericMethod(idType, converterOpenType.MakeGenericType(idType))
                .Invoke(null, [configurationBuilder]);
        }
    }

    private static void ConfigureStronglyTypedId<TId, TConverter>(
        ModelConfigurationBuilder configurationBuilder)
        where TId : class, IStronglyTypedId
        where TConverter : ValueConverter
    {
        configurationBuilder
            .Properties<TId>()
            .HaveConversion<TConverter>();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditableInterceptor, domainEventInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Owned<Slug>();
        modelBuilder.Owned<BrandSlug>();
        modelBuilder.Owned<CategorySlug>();
        modelBuilder.Owned<ProductSlug>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DBContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
