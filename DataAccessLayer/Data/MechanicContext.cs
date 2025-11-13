namespace DataAccessLayer.Data;

public class MechanicContext : DbContext
{
    public MechanicContext(DbContextOptions<MechanicContext> options)
        : base(options)
    {
    }

    public DbSet<TCarts> TCarts { get; set; }
    public DbSet<TCartItems> TCartItems { get; set; }
    public DbSet<TCategory> TCategory { get; set; }
    public DbSet<TCategoryGroup> TCategoryGroup { get; set; }
    public DbSet<TDiscountCode> TDiscountCode { get; set; }
    public DbSet<TDiscountRestriction> TDiscountRestriction { get; set; }
    public DbSet<TDiscountUsage> TDiscountUsage { get; set; }
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
    public DbSet<TUserAddress> TUserAddress { get; set; }
    public DbSet<TMedia> TMedia { get; set; }
    public DbSet<TInventoryTransaction> TInventoryTransaction { get; set; }
    public DbSet<TPaymentTransaction> TPaymentTransaction { get; set; }
    public DbSet<TProductReview> TProductReview { get; set; }
    public DbSet<TNotification> TNotification { get; set; }
    public DbSet<TUserSession> TUserSession { get; set; }
    public DbSet<TRateLimit> TRateLimit { get; set; }

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
        ConfigureSecurity(builder);
    }

    private static void ConfigureSecurity(ModelBuilder builder)
    {
        builder.Entity<TRateLimit>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
        });
    }

    private static void ConfigureSession(ModelBuilder builder)
    {
        builder.Entity<TUserSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

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
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

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
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

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
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

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
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

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
        builder.Entity<TMedia>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
        });
    }

    private static void ConfigureCart(ModelBuilder builder)
    {
        builder.Entity<TCarts>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<TCarts>(x => x.UserId)
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

            entity.HasMany(x => x.CategoryGroups)
                .WithOne(cg => cg.Category)
                .HasForeignKey(cg => cg.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Images)
                .WithOne()
                .HasForeignKey("EntityId")
                .HasPrincipalKey(e => e.Id);
        });

        builder.Entity<TCategoryGroup>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.HasMany(x => x.Products)
                .WithOne(p => p.CategoryGroup)
                .HasForeignKey(p => p.CategoryGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Images)
                .WithOne()
                .HasForeignKey("EntityId")
                .HasPrincipalKey(e => e.Id);
        });
    }

    private static void ConfigureDiscount(ModelBuilder builder)
    {
        builder.Entity<TDiscountCode>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.HasMany(d => d.Restrictions)
                .WithOne(dr => dr.DiscountCode)
                .HasForeignKey(dr => dr.DiscountCodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(d => d.Usages)
                .WithOne(du => du.DiscountCode)
                .HasForeignKey(du => du.DiscountCodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TDiscountRestriction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        builder.Entity<TDiscountUsage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

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
        });
    }

    private static void ConfigureOrder(ModelBuilder builder)
    {
        builder.Entity<TOrders>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.AddressSnapshot).HasColumnType("jsonb");

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

            entity.HasOne(x => x.Variant)
                .WithMany()
                .HasForeignKey(x => x.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TOrderStatus>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

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
        });
    }

    private static void ConfigureProduct(ModelBuilder builder)
    {
        builder.Entity<TProducts>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

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
                .HasForeignKey("EntityId")
                .HasPrincipalKey(e => e.Id);
        });

        builder.Entity<TProductVariant>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.ToTable(tb => tb.HasCheckConstraint("CK_ProductVariant_PurchasePrice", "\"PurchasePrice\" >= 0"));
            entity.ToTable(tb => tb.HasCheckConstraint("CK_ProductVariant_SellingPrice", "\"SellingPrice\" >= 0"));
            entity.ToTable(tb => tb.HasCheckConstraint("CK_ProductVariant_OriginalPrice", "\"OriginalPrice\" >= 0"));

            entity.HasOne(x => x.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.VariantAttributes)
                .WithOne(va => va.Variant)
                .HasForeignKey(va => va.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Images)
                .WithOne()
                .HasForeignKey("EntityId")
                .HasPrincipalKey(e => e.Id);
        });

        builder.Entity<TProductVariantAttribute>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        builder.Entity<TAttributeType>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.HasMany(at => at.AttributeValues)
                .WithOne(av => av.AttributeType)
                .HasForeignKey(av => av.AttributeTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TAttributeValue>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
        });
    }

    private static void ConfigureUser(ModelBuilder builder)
    {
        builder.Entity<TUserOtp>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.HasOne(x => x.User)
                .WithMany(u => u.UserOtps)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TUsers>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();

            entity.HasOne<TCarts>()
                .WithOne(c => c.User)
                .HasForeignKey<TCarts>(c => c.UserId);

            entity.HasMany(x => x.UserOrders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.UserAddresses)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TUserAddress>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
        });
    }
}