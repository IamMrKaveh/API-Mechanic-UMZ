namespace Application.Context;

public class LedkaContext : DbContext
{
    public LedkaContext(DbContextOptions<LedkaContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserAddress> UserAddresses { get; set; } = null!;
    public DbSet<UserOtp> UserOtps { get; set; } = null!;
    public DbSet<UserSession> UserSessions { get; set; } = null!;

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductVariant> ProductVariants { get; set; } = null!;
    public DbSet<ProductReview> ProductReviews { get; set; } = null!;
    public DbSet<AttributeType> AttributeTypes { get; set; } = null!;
    public DbSet<AttributeValue> AttributeValues { get; set; } = null!;
    public DbSet<ProductVariantAttribute> ProductVariantAttributes { get; set; } = null!;

    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<OrderStatus> OrderStatuses { get; set; } = null!;
    public DbSet<ShippingMethod> ShippingMethods { get; set; } = null!;

    public DbSet<Cart> Carts { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;

    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<CategoryGroup> CategoryGroups { get; set; } = null!;

    public DbSet<Media> Medias { get; set; } = null!;

    public DbSet<DiscountCode> DiscountCodes { get; set; } = null!;
    public DbSet<DiscountRestriction> DiscountRestrictions { get; set; } = null!;
    public DbSet<DiscountUsage> DiscountUsages { get; set; } = null!;

    public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;

    public DbSet<Notification> Notifications { get; set; } = null!;

    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;

    public DbSet<RateLimit> RateLimits { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureUser(builder);
        ConfigureProduct(builder);
        ConfigureOrder(builder);
        ConfigureCategory(builder);
        ConfigureCart(builder);
        ConfigureDiscount(builder);
        ConfigureMedia(builder);
        ConfigureInventory(builder);
        ConfigureNotification(builder);
        ConfigureLog(builder);
        ConfigurePayment(builder);
        ConfigureAuth(builder);
        ConfigureSecurity(builder);

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

    private void ConfigureUser(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
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
    }

    private void ConfigureProduct(ModelBuilder builder)
    {
        builder.Entity<Product>(entity =>
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
            entity.Property(e => e.Sku).HasMaxLength(100);
            entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(d => d.Product).WithMany(p => p.Variants).HasForeignKey(d => d.ProductId).OnDelete(DeleteBehavior.Cascade);
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
            entity.HasOne(d => d.Product).WithMany(p => p.Reviews).HasForeignKey(d => d.ProductId);
            entity.HasOne(d => d.User).WithMany(p => p.Reviews).HasForeignKey(d => d.UserId);
            entity.HasOne(d => d.Order).WithMany().HasForeignKey(d => d.OrderId).IsRequired(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureOrder(ModelBuilder builder)
    {
        builder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AddressSnapshot).IsRequired().HasColumnType("jsonb");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalProfit).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ShippingCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FinalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(256);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasOne(d => d.User).WithMany(p => p.UserOrders).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.UserAddress).WithMany().HasForeignKey(d => d.UserAddressId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(d => d.OrderStatus).WithMany(p => p.Orders).HasForeignKey(d => d.OrderStatusId);
            entity.HasOne(d => d.ShippingMethod).WithMany(p => p.Orders).HasForeignKey(d => d.ShippingMethodId);
            entity.HasOne(d => d.DiscountCode).WithMany(p => p.Orders).HasForeignKey(d => d.DiscountCodeId).IsRequired(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Profit).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems).HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.Variant).WithMany().HasForeignKey(d => d.VariantId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrderStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        builder.Entity<ShippingMethod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Cost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EstimatedDeliveryTime).HasMaxLength(100);
        });
    }

    private void ConfigureCategory(ModelBuilder builder)
    {
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<CategoryGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasOne(d => d.Category).WithMany(p => p.CategoryGroups).HasForeignKey(d => d.CategoryId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureCart(ModelBuilder builder)
    {
        builder.Entity<Cart>(entity =>
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
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<DiscountRestriction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RestrictionType).IsRequired().HasMaxLength(50);
            entity.HasOne(d => d.DiscountCode).WithMany(p => p.Restrictions).HasForeignKey(d => d.DiscountCodeId);
        });

        builder.Entity<DiscountUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(d => d.User).WithMany(p => p.DiscountUsages).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.DiscountCode).WithMany(p => p.Usages).HasForeignKey(d => d.DiscountCodeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Order).WithMany(p => p.DiscountUsages).HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.UsedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureMedia(ModelBuilder builder)
    {
        builder.Entity<Media>(entity =>
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
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureNotification(ModelBuilder builder)
    {
        builder.Entity<Notification>(entity =>
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
            entity.Property(e => e.Authority).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Gateway).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CardPan).HasMaxLength(20);
            entity.Property(e => e.CardHash).HasMaxLength(256);
            entity.Property(e => e.Fee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.ErrorMessage).HasMaxLength(256);
            entity.HasIndex(e => e.Authority).IsUnique();
            entity.HasOne(d => d.Order).WithMany(p => p.PaymentTransactions).HasForeignKey(d => d.OrderId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
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
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    private void ConfigureSecurity(ModelBuilder builder)
    {
        builder.Entity<RateLimit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(256);
            entity.Property(e => e.LastAttempt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }
}