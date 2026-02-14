using Domain.Payment.ValueObjects;

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

    public DbSet<Domain.User.User> Users { get; set; } = null!;
    public DbSet<UserAddress> UserAddresses { get; set; } = null!;
    public DbSet<UserOtp> UserOtps { get; set; } = null!;
    public DbSet<UserSession> UserSessions { get; set; } = null!;
    public DbSet<Wishlist> Wishlists { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<TicketMessage> TicketMessages { get; set; } = null!;

    public DbSet<Domain.Product.Product> Products { get; set; } = null!;
    public DbSet<ProductVariant> ProductVariants { get; set; } = null!;
    public DbSet<ProductReview> ProductReviews { get; set; } = null!;
    public DbSet<AttributeType> AttributeTypes { get; set; } = null!;
    public DbSet<AttributeValue> AttributeValues { get; set; } = null!;
    public DbSet<ProductVariantAttribute> ProductVariantAttributes { get; set; } = null!;
    public DbSet<ProductVariantShippingMethod> ProductVariantShippingMethods { get; set; } = null!;

    public DbSet<Domain.Order.Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<ShippingMethod> ShippingMethods { get; set; } = null!;

    public DbSet<Domain.Cart.Cart> Carts { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;

    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<CategoryGroup> CategoryGroups { get; set; } = null!;

    public DbSet<Domain.Media.Media> Medias { get; set; } = null!;

    public DbSet<DiscountCode> DiscountCodes { get; set; } = null!;
    public DbSet<DiscountRestriction> DiscountRestrictions { get; set; } = null!;
    public DbSet<DiscountUsage> DiscountUsages { get; set; } = null!;

    public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;

    public DbSet<Domain.Notification.Notification> Notifications { get; set; } = null!;

    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;

    public DbSet<Application.Security.Features.Shared.RateLimit> RateLimits { get; set; } = null!;

    public DbSet<FailedElasticOperation> FailedElasticOperations { get; set; } = null!;

    public DbSet<ElasticsearchOutboxMessage> ElasticsearchOutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureAuth(builder);
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
        ConfigureSecurity(builder);
        ConfigureSupport(builder);
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
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Sku).HasMaxLength(100);
            entity.Property(e => e.MinPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(d => d.CategoryGroup).WithMany(p => p.Products).HasForeignKey(d => d.CategoryGroupId);
            entity.Ignore(p => p.HasMultipleVariants);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Sku)
            .HasMaxLength(100);
            entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(d => d.Product).WithMany(p => p.Variants).HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.ShippingMultiplier)
            .HasPrecision(18, 2)
            .HasDefaultValue(1);
            entity.Ignore(e => e.DisplayName);
            entity.Ignore(e => e.IsInStock);
            entity.Ignore(e => e.HasDiscount);
            entity.Ignore(e => e.DiscountPercentage);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<AttributeType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<AttributeValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayValue).IsRequired().HasMaxLength(100);
            entity.Property(e => e.HexCode).HasMaxLength(7);
            entity.HasOne(d => d.AttributeType).WithMany(p => p.AttributeValues).HasForeignKey(d => d.AttributeTypeId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<ProductVariantAttribute>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(d => d.Variant).WithMany(p => p.VariantAttributes).HasForeignKey(d => d.VariantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.AttributeValue).WithMany(p => p.VariantAttributes).HasForeignKey(d => d.AttributeValueId);
            entity.HasIndex(e => new { e.VariantId, e.AttributeValueId }).IsUnique();
        });

        builder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
            entity.Property(e => e.AdminReply).HasMaxLength(1000);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.HasOne(d => d.Product).WithMany(p => p.Reviews).HasForeignKey(d => d.ProductId);
            entity.HasOne(d => d.User).WithMany(p => p.Reviews).HasForeignKey(d => d.UserId);
            entity.HasOne(d => d.Order).WithMany().HasForeignKey(d => d.OrderId).IsRequired(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
            entity.HasIndex(e => new { e.ProductId, e.Status }).HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(e => e.UserId);
        });

        builder.Entity<ProductVariantShippingMethod>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.ProductVariant)
                .WithMany(v => v.ProductVariantShippingMethods)
                .HasForeignKey(e => e.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ShippingMethod)
                .WithMany()
                .HasForeignKey(e => e.ShippingMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.ProductVariantId, e.ShippingMethodId })
                .IsUnique();

            entity.HasIndex(e => e.ProductVariantId);
            entity.HasIndex(e => e.ShippingMethodId);
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureOrder(ModelBuilder builder)
    {
        builder.Entity<Domain.Order.Order>(entity =>
        {
            entity.HasKey(e => e.Id);

            // AddressSnapshot as owned type stored as JSON
            entity.Property(e => e.AddressSnapshot)
                .HasConversion(
                    v => v.ToJson(),
                    v => AddressSnapshot.FromJson(v))
                .IsRequired()
                .HasColumnType("jsonb");

            // Money Value Objects
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

            // OrderStatusValue as string
            entity.Property(e => e.Status)
                .HasConversion(
                    v => v.Value,
                    v => OrderStatusValue.FromString(v))
                .IsRequired()
                .HasMaxLength(50);

            // OrderNumber Value Object
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

            // Navigation - OrderItems accessed through aggregate root
            entity.HasMany(e => e.OrderItems)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.ShippingMethod)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShippingMethodId);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            // Computed properties ignored
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

            // Money Value Objects
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

        builder.Entity<ShippingMethod>(entity =>
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
        builder.Entity<Category>(entity =>
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

        builder.Entity<CategoryGroup>(entity =>
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
                .WithMany(p => p.CategoryGroups)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL AND \"IsDeleted\" = false");

            // یکتایی نام در محدوده Category
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

            // Optimistic Concurrency
            entity.Property(e => e.RowVersion).IsRowVersion();

            // Audit
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
            entity.Property(e => e.UpdatedAt);

            // Soft Delete
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.DeletedAt);
            entity.Property(e => e.DeletedBy);

            // Indexes for performance
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.IsActive, e.IsDeleted, e.ExpiresAt })
                .HasFilter("\"IsDeleted\" = false");

            // Navigation to Restrictions (owned by aggregate)
            entity.HasMany(e => e.Restrictions)
                .WithOne(e => e.DiscountCode)
                .HasForeignKey(e => e.DiscountCodeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Navigation to Usages (owned by aggregate)
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

            // Index for quick lookup by type and entity
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

            // Foreign Keys
            entity.HasOne(d => d.User)
                .WithMany(p => p.DiscountUsages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Order)
                .WithMany(p => p.DiscountUsages)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
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
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.HasOne(d => d.Variant).WithMany(p => p.InventoryTransactions).HasForeignKey(d => d.VariantId);
            entity.HasOne(d => d.OrderItem).WithMany(p => p.InventoryTransactions).HasForeignKey(d => d.OrderItemId).IsRequired(false);
            entity.HasOne(d => d.User).WithMany(p => p.InventoryTransactions).HasForeignKey(d => d.UserId).IsRequired(false);
            entity.Ignore(e => e.StockAfter);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
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

            // Authority - unique index
            entity.Property(e => e.Authority)
                .IsRequired()
                .HasMaxLength(100);
            entity.HasIndex(e => e.Authority).IsUnique();

            // Money Value Object - Amount
            entity.Property(e => e.Amount)
                .HasConversion(
                    v => v.Amount,
                    v => Money.FromDecimal(v, "IRR"))
                .HasColumnType("decimal(18,2)");

            // PaymentStatus Value Object
            entity.Property(e => e.Status)
                .HasConversion(
                    v => v.Value,
                    v => PaymentStatus.FromString(v))
                .IsRequired()
                .HasMaxLength(50);

            // Gateway
            entity.Property(e => e.Gateway)
                .IsRequired()
                .HasMaxLength(50);

            // Card Info
            entity.Property(e => e.CardPan).HasMaxLength(20);
            entity.Property(e => e.CardHash).HasMaxLength(256);

            // Fee
            entity.Property(e => e.Fee).HasColumnType("decimal(18,2)");

            // IP Address
            entity.Property(e => e.IpAddress).HasMaxLength(45);

            // Error & Description
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);

            // Raw Data (large text)
            entity.Property(e => e.RawRequest).HasColumnType("text");
            entity.Property(e => e.RawResponse).HasColumnType("text");

            // Timestamps
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

            // Indexes for performance
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Status, e.ExpiresAt })
                .HasFilter("\"IsDeleted\" = false");

            // Navigation to Order
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