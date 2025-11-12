namespace DataAccessLayer.Data;

public class MechanicContext : DbContext
{
    public MechanicContext(DbContextOptions<MechanicContext> options)
        : base(options)
    {
    }

    public DbSet<TRefreshToken> TRefreshToken { get; set; }
    public DbSet<TCarts> TCarts { get; set; }
    public DbSet<TCartItems> TCartItems { get; set; }
    public DbSet<TCategory> TCategory { get; set; }
    public DbSet<TCategoryGroup> TCategoryGroup { get; set; }
    public DbSet<TDiscountCode> TDiscountCode { get; set; }
    public DbSet<TAuditLogs> TAuditLogs { get; set; }
    public DbSet<TOrders> TOrders { get; set; }
    public DbSet<TOrderItems> TOrderItems { get; set; }
    public DbSet<TOrderStatus> TOrderStatus { get; set; }
    public DbSet<TShippingMethod> TShippingMethod { get; set; }
    public DbSet<TProducts> TProducts { get; set; }
    public DbSet<TProductVariant> TProductVariant { get; set; }
    public DbSet<TAttributeType> TAttributeType { get; set; }
    public DbSet<TAttributeValue> TAttributeValue { get; set; }
    public DbSet<TProductVariantAttribute> TProductVariantAttribute { get; set; }
    public DbSet<TUserOtp> TUserOtp { get; set; }
    public DbSet<TUsers> TUsers { get; set; }
    public DbSet<TMedia> TMedia { get; set; }
    public DbSet<TInventoryTransaction> TInventoryTransaction { get; set; }
    public DbSet<TUserAddress> TUserAddress { get; set; }
    public DbSet<TPaymentTransaction> TPaymentTransaction { get; set; }
    public DbSet<TProductReview> TProductReview { get; set; }
    public DbSet<TNotification> TNotification { get; set; }
    public DbSet<TDiscountRestriction> TDiscountRestriction { get; set; }
    public DbSet<TDiscountUsage> TDiscountUsage { get; set; }
    public DbSet<TUserSession> TUserSession { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var notIsDeleted = Expression.Not(property);
                var lambda = Expression.Lambda(notIsDeleted, parameter);

                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        ConfigureAuth(builder);
        ConfigureCart(builder);
        ConfigureCategory(builder);
        ConfigureDiscount(builder);
        ConfigureLog(builder);
        ConfigureOrder(builder);
        ConfigureProduct(builder);
        ConfigureUser(builder);
        ConfigureMedia(builder);
        ConfigureInventory(builder);
        ConfigurePayment(builder);
        ConfigureReview(builder);
        ConfigureNotification(builder);
        ConfigureSession(builder);
    }

    private static void ConfigureSession(ModelBuilder builder)
    {
        builder.Entity<TUserSession>(entity =>
        {
            entity.HasOne(s => s.User)
                .WithMany(u => u.UserSessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureNotification(ModelBuilder builder)
    {
        builder.Entity<TNotification>(entity =>
        {
            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    private static void ConfigureReview(ModelBuilder builder)
    {
        builder.Entity<TProductReview>(entity =>
        {
            entity.HasOne(pr => pr.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pr => pr.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pr => pr.Order)
                .WithMany()
                .HasForeignKey(pr => pr.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigurePayment(ModelBuilder builder)
    {
        builder.Entity<TPaymentTransaction>(entity =>
        {
            entity.HasOne(pt => pt.Order)
                .WithMany(o => o.PaymentTransactions)
                .HasForeignKey(pt => pt.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureInventory(ModelBuilder builder)
    {
        builder.Entity<TInventoryTransaction>(entity =>
        {
            entity.HasOne(it => it.Variant)
                .WithMany(v => v.InventoryTransactions)
                .HasForeignKey(it => it.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(it => it.OrderItem)
                .WithMany()
                .HasForeignKey(it => it.OrderItemId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(it => it.User)
                .WithMany()
                .HasForeignKey(it => it.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureMedia(ModelBuilder builder)
    {
        builder.Entity<TMedia>();
    }

    private static void ConfigureAuth(ModelBuilder builder)
    {
        builder.Entity<TRefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(512);
            entity.Property(x => x.CreatedByIp).HasMaxLength(45);
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(512);
            entity.Property(x => x.UserAgent).HasMaxLength(255);

            entity.HasOne(x => x.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCart(ModelBuilder builder)
    {
        builder.Entity<TCarts>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.GuestToken).HasMaxLength(100);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.LastUpdated).IsRequired();

            entity.HasOne(x => x.User)
                  .WithMany(u => u.UserCarts)
                  .HasForeignKey(x => x.UserId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.CartItems)
                  .WithOne(ci => ci.Cart)
                  .HasForeignKey(ci => ci.CartId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TCartItems>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Quantity).IsRequired();
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasOne(x => x.Variant)
                  .WithMany()
                  .HasForeignKey(x => x.VariantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCategory(ModelBuilder builder)
    {
        builder.Entity<TCategory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);

            entity.HasMany(x => x.CategoryGroups)
                  .WithOne(cg => cg.Category)
                  .HasForeignKey(cg => cg.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Images)
               .WithOne()
               .HasForeignKey(m => m.EntityId)
               .HasPrincipalKey(e => e.Id);
        });

        builder.Entity<TCategoryGroup>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CategoryId).IsRequired();

            entity.HasMany(x => x.Products)
                  .WithOne(p => p.CategoryGroup)
                  .HasForeignKey(p => p.CategoryGroupId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Images)
                .WithOne()
                .HasForeignKey(m => m.EntityId)
                .HasPrincipalKey(e => e.Id);
        });
    }

    private static void ConfigureDiscount(ModelBuilder builder)
    {
        builder.Entity<TDiscountCode>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Code).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Percentage).HasColumnType("decimal(5,2)").IsRequired();
            entity.Property(x => x.MaxDiscountAmount).HasColumnType("decimal(19,4)");
            entity.Property(x => x.MinOrderAmount).HasColumnType("decimal(19,4)");
            entity.Property(x => x.UsageLimit).IsRequired(false);
            entity.Property(x => x.UsedCount).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.ExpiresAt).IsRequired(false);

            entity.HasMany(d => d.Restrictions)
                .WithOne(dr => dr.DiscountCode)
                .HasForeignKey(dr => dr.DiscountCodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(d => d.Usages)
                .WithOne(du => du.DiscountCode)
                .HasForeignKey(du => du.DiscountCodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TDiscountUsage>(entity =>
        {
            entity.HasOne(du => du.Order)
                .WithMany(o => o.DiscountUsages)
                .HasForeignKey(du => du.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(du => du.User)
                .WithMany()
                .HasForeignKey(du => du.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureLog(ModelBuilder builder)
    {
        builder.Entity<TAuditLogs>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Action).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Details).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(100);
            entity.Property(x => x.UserAgent).HasMaxLength(300);
            entity.Property(x => x.Timestamp).IsRequired();
        });
    }

    private static void ConfigureOrder(ModelBuilder builder)
    {
        builder.Entity<TOrders>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.AddressSnapshot).HasColumnType("jsonb");
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(19,4)").HasDefaultValue(0);
            entity.Property(x => x.TotalProfit).HasColumnType("decimal(19,4)").HasDefaultValue(0);
            entity.Property(x => x.ShippingCost).HasColumnType("decimal(19,4)").HasDefaultValue(0);
            entity.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(100);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.Property(o => o.FinalAmount)
                  .HasComputedColumnSql("\"TotalAmount\" + \"ShippingCost\" - \"DiscountAmount\"", stored: true);

            entity.HasOne(x => x.User)
                  .WithMany(u => u.UserOrders)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UserAddress)
                .WithMany()
                .HasForeignKey(x => x.UserAddressId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.OrderStatus)
                  .WithMany(os => os.Orders)
                  .HasForeignKey(x => x.OrderStatusId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ShippingMethod)
                  .WithMany(sm => sm.Orders)
                  .HasForeignKey(x => x.ShippingMethodId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.OrderItems)
                  .WithOne(oi => oi.Order)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TOrderItems>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.PurchasePrice).IsRequired().HasColumnType("decimal(19,4)");
            entity.Property(x => x.SellingPrice).IsRequired().HasColumnType("decimal(19,4)");
            entity.Property(x => x.Quantity).IsRequired();
            entity.Property(x => x.Amount).HasComputedColumnSql("\"SellingPrice\" * \"Quantity\"", stored: true).HasColumnType("decimal(19,4)");
            entity.Property(x => x.Profit).HasComputedColumnSql("(\"SellingPrice\" - \"PurchasePrice\") * \"Quantity\"", stored: true).HasColumnType("decimal(19,4)");
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasOne(x => x.Variant)
                .WithMany()
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TOrderStatus>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);

            entity.HasData(
                new TOrderStatus { Id = 1, Name = "در انتظار پرداخت" },
                new TOrderStatus { Id = 2, Name = "در حال پردازش" },
                new TOrderStatus { Id = 3, Name = "ارسال شده" },
                new TOrderStatus { Id = 4, Name = "تحویل داده شده" },
                new TOrderStatus { Id = 5, Name = "لغو شده" },
                new TOrderStatus { Id = 6, Name = "مرجوعی" }
            );
        });

        builder.Entity<TShippingMethod>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Cost).HasColumnType("decimal(19,4)").HasDefaultValue(0);
        });
    }

    private static void ConfigureProduct(ModelBuilder builder)
    {
        builder.Entity<TProducts>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasOne(x => x.CategoryGroup)
                  .WithMany(cg => cg.Products)
                  .HasForeignKey(x => x.CategoryGroupId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Images)
                .WithOne()
                .HasForeignKey(m => m.EntityId)
                .HasPrincipalKey(e => e.Id);
        });

        builder.Entity<TProductVariant>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.PurchasePrice).HasColumnType("decimal(19,4)");
            entity.Property(x => x.OriginalPrice).HasColumnType("decimal(19,4)");
            entity.Property(x => x.SellingPrice).HasColumnType("decimal(19,4)");
            entity.Property(x => x.Stock).HasDefaultValue(0);

            entity.ToTable(tb => tb.HasCheckConstraint("CK_ProductVariant_PurchasePrice", "\"PurchasePrice\" >= 0"));
            entity.ToTable(tb => tb.HasCheckConstraint("CK_ProductVariant_SellingPrice", "\"SellingPrice\" > 0"));
            entity.ToTable(tb => tb.HasCheckConstraint("CK_ProductVariant_OriginalPrice", "\"OriginalPrice\" >= 0"));

            entity.HasOne(x => x.Product)
                  .WithMany(p => p.Variants)
                  .HasForeignKey(x => x.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.VariantAttributes)
                .WithOne(va => va.Variant)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Images)
                .WithOne()
                .HasForeignKey(m => m.EntityId)
                .HasPrincipalKey(e => e.Id);
        });

        builder.Entity<TProductVariantAttribute>()
            .HasIndex(va => new { va.VariantId, va.AttributeValueId })
            .IsUnique();

        builder.Entity<TAttributeType>()
            .HasMany(at => at.AttributeValues)
            .WithOne(av => av.AttributeType)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUser(ModelBuilder builder)
    {
        builder.Entity<TUserOtp>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.OtpHash).IsRequired().HasMaxLength(512);
            entity.Property(x => x.ExpiresAt).IsRequired();
            entity.Property(x => x.IsUsed).HasDefaultValue(false);
            entity.Property(x => x.AttemptCount).HasDefaultValue(0);

            entity.HasOne(x => x.User)
                  .WithMany(u => u.UserOtps)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TUsers>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(15);
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.IsAdmin).HasDefaultValue(false);
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);

            entity.HasMany(x => x.UserOrders)
                  .WithOne(o => o.User)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.UserAddresses)
                  .WithOne(o => o.User)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}