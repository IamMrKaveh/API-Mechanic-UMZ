using Application.Order.Sagas.State;
using Application.Search.Features.Shared;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Domain.Attribute.ValueObjects;
using Domain.Audit.Entities;
using Domain.Audit.ValueObjects;
using Domain.Brand.ValueObjects;
using Domain.Cart.Entities;
using Domain.Cart.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Discount.Aggregates;
using Domain.Discount.ValueObjects;
using Domain.Inventory.Aggregates;
using Domain.Inventory.Entities;
using Domain.Inventory.ValueObjects;
using Domain.Media.ValueObjects;
using Domain.Notification.ValueObjects;
using Domain.Order.Entities;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.ValueObjects;
using Domain.Security.Aggregates;
using Domain.Security.ValueObjects;
using Domain.Shipping.ValueObjects;
using Domain.Support.Aggregates;
using Domain.Support.Entities;
using Domain.Support.ValueObjects;
using Domain.User.Entities;
using Domain.User.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Entities;
using Domain.Variant.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.ValueObjects;
using Domain.Wishlist.ValueObjects;
using Infrastructure.Attribute.Converters;
using Infrastructure.Audit.Converters;
using Infrastructure.Brand.Converters;
using Infrastructure.Cart.Converters;
using Infrastructure.Category.Converters;
using Infrastructure.Discount.Converters;
using Infrastructure.Inventory.Converters;
using Infrastructure.Media.Converters;
using Infrastructure.Notification.Converters;
using Infrastructure.Order.Converters;
using Infrastructure.Payment.Converters;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Infrastructure.Product.Converters;
using Infrastructure.Review.Converters;
using Infrastructure.Search;
using Infrastructure.Security.Converters;
using Infrastructure.Shipping.Converters;
using Infrastructure.Support.Converters;
using Infrastructure.User.Converters;
using Infrastructure.Variant.Converters;
using Infrastructure.Wallet.Converters;
using Infrastructure.Wishlist.Converters;

namespace Infrastructure.Persistence.Context;

public sealed class DBContext(
    DbContextOptions<DBContext> options,
    AuditableEntityInterceptor auditableInterceptor,
    DomainEventInterceptor domainEventInterceptor) : DbContext(options)
{
    public DbSet<Domain.User.Aggregates.User> Users => Set<Domain.User.Aggregates.User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<UserOtp> UserOtps => Set<UserOtp>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AttributeType> AttributeTypes => Set<AttributeType>();
    public DbSet<AttributeValue> AttributeValues => Set<AttributeValue>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Domain.Brand.Aggregates.Brand> Brands => Set<Domain.Brand.Aggregates.Brand>();
    public DbSet<Domain.Cart.Aggregates.Cart> Carts => Set<Domain.Cart.Aggregates.Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Domain.Category.Aggregates.Category> Categories => Set<Domain.Category.Aggregates.Category>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Domain.Inventory.Aggregates.Inventory> Inventories => Set<Domain.Inventory.Aggregates.Inventory>();
    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();
    public DbSet<Domain.Media.Aggregates.Media> Medias => Set<Domain.Media.Aggregates.Media>();
    public DbSet<Domain.Notification.Aggregates.Notification> Notifications => Set<Domain.Notification.Aggregates.Notification>();
    public DbSet<Domain.Order.Aggregates.Order> Orders => Set<Domain.Order.Aggregates.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Domain.Product.Aggregates.Product> Products => Set<Domain.Product.Aggregates.Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<VariantAttribute> ProductVariantAttributes => Set<VariantAttribute>();
    public DbSet<VariantShipping> ProductVariantShippings => Set<VariantShipping>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<Domain.Shipping.Aggregates.Shipping> Shippings => Set<Domain.Shipping.Aggregates.Shipping>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<Domain.Wallet.Aggregates.Wallet> Wallets => Set<Domain.Wallet.Aggregates.Wallet>();
    public DbSet<WalletLedgerEntry> WalletLedgerEntries => Set<WalletLedgerEntry>();
    public DbSet<WalletReservation> WalletReservations => Set<WalletReservation>();
    public DbSet<Domain.Wishlist.Aggregates.Wishlist> Wishlists => Set<Domain.Wishlist.Aggregates.Wishlist>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ElasticsearchOutboxMessage> ElasticsearchOutboxMessages => Set<ElasticsearchOutboxMessage>();
    public DbSet<FailedElasticOperation> FailedElasticOperations => Set<FailedElasticOperation>();
    public DbSet<OrderProcessState> OrderProcessStates => Set<OrderProcessState>();
    public DbSet<RateLimitEntry> RateLimitEntries => Set<RateLimitEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ConfigureStronglyTypedId<AttributeTypeId, AttributeTypeIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<AttributeValueId, AttributeValueIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<AuditLogId, AuditLogIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<BrandId, BrandIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<CartId, CartIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<CartItemId, CartItemIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<CategoryId, CategoryIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<DiscountCodeId, DiscountCodeIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<DiscountRestrictionId, DiscountRestrictionIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<DiscountUsageId, DiscountUsageIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<InventoryId, InventoryIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<StockLedgerEntryId, StockLedgerEntryIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<WarehouseId, WarehouseIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<MediaId, MediaIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<NotificationId, NotificationIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<OrderId, OrderIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<OrderItemId, OrderItemIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<OrderStatusId, OrderStatusIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<PaymentMethodId, PaymentMethodIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<PaymentTransactionId, PaymentTransactionIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<ProductId, ProductIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<ReviewId, ReviewIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<OtpId, OtpIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<SessionId, SessionIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<ShippingId, ShippingIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<TicketId, TicketIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<TicketMessageId, TicketMessageIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<UserAddressId, UserAddressIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<UserId, UserIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<VariantAttributeId, VariantAttributeIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<VariantId, VariantIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<VariantShippingId, VariantShippingIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<WalletId, WalletIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<WalletLedgerEntryId, WalletLedgerEntryIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<WalletReservationId, WalletReservationIdConverter>(configurationBuilder);
        ConfigureStronglyTypedId<WishlistId, WishlistIdConverter>(configurationBuilder);

        base.ConfigureConventions(configurationBuilder);
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