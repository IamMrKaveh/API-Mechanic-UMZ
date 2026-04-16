using Application.Search.Features.Shared;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Domain.Audit.Entities;
using Domain.Cart.Entities;
using Domain.Discount.Aggregates;
using Domain.Discount.Entities;
using Domain.Inventory.Aggregates;
using Domain.Inventory.Entities;
using Domain.Order.Entities;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Review.Aggregates;
using Domain.Security.Aggregates;
using Domain.Support.Aggregates;
using Domain.Support.Entities;
using Domain.User.Entities;
using Domain.Variant.Aggregates;
using Domain.Variant.Entities;
using Domain.Wallet.Entities;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Infrastructure.Search;
using Infrastructure.Security.Models;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<DiscountRestriction> DiscountRestrictions => Set<DiscountRestriction>();
    public DbSet<DiscountUsage> DiscountUsages => Set<DiscountUsage>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Domain.Inventory.Aggregates.Inventory> Inventories => Set<Domain.Inventory.Aggregates.Inventory>();
    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();
    public DbSet<Domain.Media.Aggregates.Media> Medias => Set<Domain.Media.Aggregates.Media>();
    public DbSet<Domain.Notification.Aggregates.Notification> Notifications => Set<Domain.Notification.Aggregates.Notification>();
    public DbSet<Domain.Order.Aggregates.Order> Orders => Set<Domain.Order.Aggregates.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Domain.Order.Entities.OrderStatus> OrderStatuses => Set<Domain.Order.Entities.OrderStatus>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Domain.Product.Aggregates.Product> Products => Set<Domain.Product.Aggregates.Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantAttribute> ProductVariantAttributes => Set<ProductVariantAttribute>();
    public DbSet<ProductVariantShipping> ProductVariantShippings => Set<ProductVariantShipping>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<Domain.Shipping.Aggregates.Shipping> Shippings => Set<Domain.Shipping.Aggregates.Shipping>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<Domain.Wallet.Aggregates.Wallet> Wallets => Set<Domain.Wallet.Aggregates.Wallet>();
    public DbSet<WalletLedgerEntry> WalletLedgerEntries => Set<WalletLedgerEntry>();
    public DbSet<Domain.Wishlist.Aggregates.Wishlist> Wishlists => Set<Domain.Wishlist.Aggregates.Wishlist>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ElasticsearchOutboxMessage> ElasticsearchOutboxMessages => Set<ElasticsearchOutboxMessage>();
    public DbSet<FailedElasticOperation> FailedElasticOperations => Set<FailedElasticOperation>();
    public DbSet<OrderProcessState> OrderProcessStates => Set<OrderProcessState>();
    public DbSet<RateLimitEntry> RateLimitEntries => Set<RateLimitEntry>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditableInterceptor, domainEventInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DBContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}