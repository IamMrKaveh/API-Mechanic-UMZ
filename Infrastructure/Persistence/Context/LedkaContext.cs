using Domain.Brand;

namespace Infrastructure.Persistence.Context;

public class LedkaContext : DbContext
{
    public LedkaContext(DbContextOptions<LedkaContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    public DbSet<Domain.User.User> Users => Set<Domain.User.User>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<UserOtp> UserOtps => Set<UserOtp>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();

    public DbSet<Domain.Product.Product> Products => Set<Domain.Product.Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<AttributeType> AttributeTypes => Set<AttributeType>();
    public DbSet<AttributeValue> AttributeValues => Set<AttributeValue>();
    public DbSet<ProductVariantAttribute> ProductVariantAttributes => Set<ProductVariantAttribute>();
    public DbSet<ProductVariantShipping> ProductVariantShippingMethods => Set<ProductVariantShipping>();

    public DbSet<Domain.Order.Order> Orders => Set<Domain.Order.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Domain.Shipping.Shipping> ShippingMethods => Set<Domain.Shipping.Shipping>();

    public DbSet<Domain.Cart.Cart> Carts => Set<Domain.Cart.Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Domain.Category.Category> Categories => Set<Domain.Category.Category>();
    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Domain.Media.Media> Medias => Set<Domain.Media.Media>();

    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<DiscountRestriction> DiscountRestrictions => Set<DiscountRestriction>();
    public DbSet<DiscountUsage> DiscountUsages => Set<DiscountUsage>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<Domain.Notification.Notification> Notifications => Set<Domain.Notification.Notification>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<Application.Security.Features.Shared.RateLimit> RateLimits
        => Set<Application.Security.Features.Shared.RateLimit>();

    public DbSet<FailedElasticOperation> FailedElasticOperations => Set<FailedElasticOperation>();
    public DbSet<ElasticsearchOutboxMessage> ElasticsearchOutboxMessages => Set<ElasticsearchOutboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureAuth(builder);
        ConfigureAuditLog(builder);
        ConfigureCart(builder);
        ConfigureCategory(builder);
        ConfigureDiscount(builder);
        ConfigureElasticSearch(builder);
        ConfigureInventory(builder);
        ConfigureLog(builder);
        ConfigureMedia(builder);
        ConfigureNotification(builder);
        ConfigureOrder(builder);
        ConfigurePayment(builder);
        ConfigureProduct(builder);
        ConfigureVariant(builder);
        ConfigureAttribute(builder);
        ConfigureReview(builder);
        ConfigureSecurity(builder);
        ConfigureSupport(builder);
        ConfigureCommon(builder);
        ConfigureUser(builder);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(LedkaContext).GetMethod(nameof(ApplySoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    !.MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, [builder]);
            }
        }
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder builder)
    where TEntity : class, ISoftDeletable
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    private void ConfigureAuditLog(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(e =>
        {
            // ستون‌های جدید برای AuditLog
            e.Property(x => x.IntegrityHash).HasMaxLength(100).IsRequired();
            e.Property(x => x.IsArchived).HasDefaultValue(false);
            e.Property(x => x.ArchivedAt);
            e.HasIndex(x => x.IsArchived);
            e.HasIndex(x => x.EventType);
            e.HasIndex(x => new { x.UserId, x.Timestamp });
        });
    }

    private void ConfigureElasticSearch(ModelBuilder builder)
    {
        builder.Entity<FailedElasticOperation>(entity =>
        {
            entity.ToTable("FailedElasticOperations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Document).IsRequired();
            entity.Property(e => e.Error).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        builder.Entity<ElasticsearchOutboxMessage>(entity =>
        {
            entity.ToTable("ElasticsearchOutboxMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(50);
        });
    }

    private void ConfigureUser(ModelBuilder builder)
    {
        builder.Entity<Domain.User.User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(15);
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<UserAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ReceiverName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(15);
            entity.Property(e => e.Province).IsRequired().HasMaxLength(50);
            entity.Property(e => e.City).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(d => d.User).WithMany(p => p.UserAddresses).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<UserOtp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OtpHash).IsRequired();
            entity.HasOne(d => d.User).WithMany(p => p.UserOtps).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Product).WithMany().HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureCommon(ModelBuilder builder)
    {
        builder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Content).IsRequired().HasColumnType("jsonb");
            entity.HasIndex(e => new { e.ProcessedOn, e.OccurredOn });
        });
    }

    private void ConfigureSupport(ModelBuilder builder)
    {
        builder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Priority).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);

            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Ticket)
                .HasForeignKey(m => m.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => new { e.Status, e.Priority });
        });

        builder.Entity<TicketMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(d => d.Ticket)
                .WithMany(p => p.Messages)
                .HasForeignKey(d => d.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TicketId);
        });
    }

    private void ConfigureProduct(ModelBuilder builder)
    {
        builder.Entity<Domain.Product.Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasConversion(v => v.Value, v => Domain.Product.ValueObjects.ProductName.Create(v)).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(d => d.Brand).WithMany(p => p.Products).HasForeignKey(d => d.BrandId);
            entity.OwnsOne(e => e.Stats, s =>
            {
                s.Property(p => p.MinPrice).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");
                s.Property(p => p.MaxPrice).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");
                s.Property(p => p.TotalStock);
                s.Property(p => p.AverageRating).HasColumnType("decimal(3,2)");
                s.Property(p => p.ReviewCount);
                s.Property(p => p.SalesCount);
            });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureVariant(ModelBuilder builder)
    {
        builder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sku).HasConversion(v => v != null ? v.Value : null, v => v != null ? Sku.Create(v) : null).HasMaxLength(100);
            entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(d => d.Product).WithMany(p => p.Variants).HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.ShippingMultiplier).HasPrecision(18, 2).HasDefaultValue(1);
            entity.Ignore(e => e.DisplayName);
            entity.Ignore(e => e.IsInStock);
            entity.Ignore(e => e.HasDiscount);
            entity.Ignore(e => e.DiscountPercentage);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasIndex(e => new { e.ProductId, e.Sku }).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(e => new { e.IsDeleted, e.IsActive, e.StockQuantity });

            entity.HasIndex(e => new { e.SellingPrice, e.IsActive, e.IsDeleted });
        });

        builder.Entity<ProductVariantShipping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.ProductVariant).WithMany(v => v.ProductVariantShippingMethods).HasForeignKey(e => e.ProductVariantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Shipping).WithMany().HasForeignKey(e => e.ShippingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ProductVariantId, e.ShippingId }).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureAttribute(ModelBuilder builder)
    {
        builder.Entity<AttributeType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            // Search optimization index
            entity.HasIndex(e => e.Name);
        });

        builder.Entity<AttributeValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayValue).IsRequired().HasMaxLength(100);
            entity.Property(e => e.HexCode).HasMaxLength(7);
            entity.HasOne(d => d.AttributeType).WithMany(p => p.Values).HasForeignKey(d => d.AttributeTypeId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            // Search optimization index
            entity.HasIndex(e => e.Value);
        });

        builder.Entity<ProductVariantAttribute>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne<ProductVariant>(p => p.Variant).WithMany(p => p.VariantAttributes).HasForeignKey(d => d.VariantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.AttributeValue).WithMany(p => p.VariantAttributes).HasForeignKey(d => d.AttributeValueId);
            entity.HasIndex(e => new { e.VariantId, e.AttributeValueId }).IsUnique();
        });
    }

    private void ConfigureReview(ModelBuilder builder)
    {
        builder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.AdminReply).HasMaxLength(1000);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.HasOne<Domain.Product.Product>().WithMany().HasForeignKey(d => d.ProductId);
            entity.HasOne<Domain.User.User>().WithMany().HasForeignKey(d => d.UserId);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasIndex(e => new { e.ProductId, e.Status, e.Rating })
                  .HasFilter("\"IsDeleted\" = false");

            entity.HasIndex(e => e.UserId);
        });
    }

    private void ConfigureOrder(ModelBuilder builder)
    {
        builder.Entity<Domain.Order.Order>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AddressSnapshot)
                .HasConversion(
                    v => v.ToJson(),
                    v => AddressSnapshot.FromJson(v))
                .IsRequired()
                .HasColumnType("jsonb");

            entity.Property(e => e.TotalAmount)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalProfit)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.ShippingCost)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.DiscountAmount)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.FinalAmount)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Status)
                .HasConversion(
                    v => v.Value,
                    v => OrderStatusValue.FromString(v))
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.OrderNumber)
                .HasConversion(
                    v => v.Value,
                    v => OrderNumber.FromString(v))
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ReceiverName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(256);
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasMany(e => e.OrderItems)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.ShippingMethod)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShippingMethodId);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.Ignore(e => e.IsPaid);
            entity.Ignore(e => e.IsShipped);
            entity.Ignore(e => e.IsDelivered);
            entity.Ignore(e => e.IsCancelled);
            entity.Ignore(e => e.IsPending);
            entity.Ignore(e => e.IsProcessing);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PurchasePriceAtOrder)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.SellingPriceAtOrder)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.OriginalPriceAtOrder)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.DiscountAtOrder)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Amount)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Profit)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.VariantSku).HasMaxLength(100);
            entity.Property(e => e.VariantAttributes).HasMaxLength(500);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.VariantId);
            entity.HasIndex(e => e.ProductId);
        });

        builder.Entity<Domain.Shipping.Shipping>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EstimatedDeliveryTime).HasMaxLength(100);

            entity.Property(e => e.BaseCost)
                .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxOrderAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxWeight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FreeShippingThreshold).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureCategory(ModelBuilder builder)
    {
        builder.Entity<Domain.Category.Category>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasConversion(
                    v => v.Value,
                    v => CategoryName.Create(v))
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Slug)
                .HasConversion(
                    v => v != null ? v.Value : null,
                    v => v != null ? Slug.FromString(v) : null)
                .HasMaxLength(200);

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasIndex(e => e.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL AND \"IsDeleted\" = false");
        });

        builder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasConversion(
                    v => v.Value,
                    v => CategoryName.Create(v))
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Slug)
                .HasConversion(
                    v => v != null ? v.Value : null,
                    v => v != null ? Slug.FromString(v) : null)
                .HasMaxLength(200);

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Brands)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL AND \"IsDeleted\" = false");

            entity.HasIndex(e => new { e.CategoryId, e.Name })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
        });
    }

    private void ConfigureCart(ModelBuilder builder)
    {
        builder.Entity<Domain.Cart.Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GuestToken).HasMaxLength(256);
            entity.HasIndex(e => e.UserId).IsUnique().HasFilter("\"UserId\" IS NOT NULL");
            entity.HasIndex(e => e.GuestToken).IsUnique().HasFilter("\"GuestToken\" IS NOT NULL");
            entity.HasOne(d => d.User).WithMany(p => p.UserCarts).HasForeignKey(d => d.UserId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems).HasForeignKey(d => d.CartId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Variant).WithMany(p => p.CartItems).HasForeignKey(d => d.VariantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CartId, e.VariantId }).IsUnique();
        });
    }

    private void ConfigureDiscount(ModelBuilder builder)
    {
        builder.Entity<DiscountCode>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.Percentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.MaxDiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(18,2)");

            entity.Property(e => e.UsageLimit);
            entity.Property(e => e.MaxUsagePerUser);
            entity.Property(e => e.UsedCount).HasDefaultValue(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.Property(e => e.StartsAt);
            entity.Property(e => e.ExpiresAt);

            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
            entity.Property(e => e.UpdatedAt);

            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.DeletedAt);
            entity.Property(e => e.DeletedBy);

            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.IsActive, e.IsDeleted, e.ExpiresAt })
                .HasFilter("\"IsDeleted\" = false");

            entity.HasMany(e => e.Restrictions)
                .WithOne(e => e.DiscountCode)
                .HasForeignKey(e => e.DiscountCodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Usages)
                .WithOne(e => e.DiscountCode)
                .HasForeignKey(e => e.DiscountCodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DiscountRestriction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(50)
                .HasConversion<string>();

            entity.Property(e => e.EntityId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasIndex(e => new { e.DiscountCodeId, e.Type, e.EntityId });
        });

        builder.Entity<DiscountUsage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DiscountAmount)
                .HasConversion(
                    v => v.Amount,
                    v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.UsedAt).HasDefaultValueSql("now() at time zone 'utc'");
            entity.Property(e => e.IsConfirmed).HasDefaultValue(false);
            entity.Property(e => e.ConfirmedAt);
            entity.Property(e => e.IsCancelled).HasDefaultValue(false);
            entity.Property(e => e.CancelledAt);

            entity.HasOne(d => d.User)
                .WithMany(p => p.DiscountUsages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Order)
                .WithMany(p => p.DiscountUsages)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => new { e.DiscountCodeId, e.UserId });
            entity.HasIndex(e => new { e.DiscountCodeId, e.OrderId }).IsUnique();
        });
    }

    private void ConfigureMedia(ModelBuilder builder)
    {
        builder.Entity<Domain.Media.Media>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AltText).HasMaxLength(255);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureInventory(ModelBuilder builder)
    {
        builder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TransactionType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Notes)
                .HasMaxLength(500);

            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(100);

            // FIX #13: فیلدهای جدید
            entity.Property(e => e.CorrelationId)
                .HasMaxLength(200)
                .IsRequired(false);

            entity.Property(e => e.CartId)
                .HasMaxLength(200)
                .IsRequired(false);

            entity.Property(e => e.ExpiresAt)
                .IsRequired(false);

            // ─── Unique Index برای Idempotency ─────────────────────────────
            // از ثبت مجدد رزرو/Commit در retry جلوگیری می‌کند
            entity.HasIndex(e => new { e.VariantId, e.TransactionType, e.CorrelationId })
                .IsUnique()
                .HasFilter("\"CorrelationId\" IS NOT NULL") // فقط رکوردهایی که CorrelationId دارند
                .HasDatabaseName("IX_InventoryTransactions_Idempotency");

            // ─── Index برای جستجوی رزروهای منقضی (BackgroundService) ──────
            entity.HasIndex(e => new { e.TransactionType, e.ExpiresAt, e.IsReversed })
                .HasFilter("\"ExpiresAt\" IS NOT NULL AND \"IsReversed\" = false")
                .HasDatabaseName("IX_InventoryTransactions_ExpiredReservations");

            // ─── Index برای ReferenceNumber (RollbackReservations) ─────────
            entity.HasIndex(e => new { e.ReferenceNumber, e.TransactionType, e.IsReversed })
                .HasDatabaseName("IX_InventoryTransactions_Reference");

            // ─── Index برای VariantId (query لجر واریانت) ──────────────────
            entity.HasIndex(e => e.VariantId)
                .HasDatabaseName("IX_InventoryTransactions_VariantId");

            // ─── Relationships ──────────────────────────────────────────────
            entity.HasOne(d => d.Variant)
                .WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.VariantId);

            entity.HasOne(d => d.OrderItem)
                .WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.OrderItemId)
                .IsRequired(false);

            entity.HasOne(d => d.User)
                .WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.UserId)
                .IsRequired(false);

            entity.Ignore(e => e.StockAfter);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<StockLedgerEntry>(e =>
        {
            e.ToTable("StockLedger");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.CorrelationId).HasMaxLength(100);
            e.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.VariantId);
            e.HasIndex(x => x.ReferenceNumber);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.HasIndex(x => x.CreatedAt);

            // Append-Only: جلوگیری از Update در EF Core
            e.Property<uint>("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        });

        builder.Entity<Warehouse>(e =>
        {
            e.ToTable("Warehouses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.City).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<WarehouseStock>(e =>
        {
            e.ToTable("WarehouseStocks");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.WarehouseId, x.VariantId }).IsUnique();
        });
    }

    private void ConfigureNotification(ModelBuilder builder)
    {
        builder.Entity<Domain.Notification.Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ActionUrl).HasMaxLength(255);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasForeignKey(d => d.UserId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureLog(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Details).IsRequired().HasColumnType("text");
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EventType);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigurePayment(ModelBuilder builder)
    {
        builder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Authority)
                .IsRequired()
                .HasMaxLength(100);
            entity.HasIndex(e => e.Authority).IsUnique();

            entity.Property(e => e.Amount)
                .HasConversion(
                    v => v.Amount,
                    v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Status)
                .HasConversion(
                    v => v.Value,
                    v => PaymentStatus.FromString(v))
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Gateway)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CardPan).HasMaxLength(20);
            entity.Property(e => e.CardHash).HasMaxLength(256);

            entity.Property(e => e.Fee).HasColumnType("decimal(18,2)");

            entity.Property(e => e.IpAddress).HasMaxLength(45);

            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.Property(e => e.RawRequest).HasColumnType("text");
            entity.Property(e => e.RawResponse).HasColumnType("text");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Status, e.ExpiresAt })
                .HasFilter("\"IsDeleted\" = false");

            entity.HasOne(d => d.Order)
                .WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.OrderId);
        });
    }

    private void ConfigureAuth(ModelBuilder builder)
    {
        builder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenSelector).IsRequired().HasMaxLength(256);
            entity.Property(e => e.TokenVerifierHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.CreatedByIp).IsRequired().HasMaxLength(45);
            entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(256);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.SessionType).HasMaxLength(50);
            entity.HasIndex(e => e.TokenSelector).IsUnique();
            entity.HasOne(d => d.User).WithMany(p => p.UserSessions).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.Ignore(e => e.IsActive);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureSecurity(ModelBuilder builder)
    {
        builder.Entity<Application.Security.Features.Shared.RateLimit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(256);
            entity.Property(e => e.LastAttempt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }
}