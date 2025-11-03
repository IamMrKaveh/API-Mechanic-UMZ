namespace DataAccessLayer.Data;

public class MechanicContext : DbContext
{
    public MechanicContext(DbContextOptions<MechanicContext> options)
        : base(options)
    {
    }

    #region DbSets

    #region Auth

    public DbSet<TRefreshToken> TRefreshToken { get; set; }

    #endregion

    #region Cart

    public DbSet<TCarts> TCarts { get; set; }
    public DbSet<TCartItems> TCartItems { get; set; }

    #endregion

    #region Log

    public DbSet<TAuditLogs> TAuditLogs { get; set; }

    #endregion

    #region Order

    public DbSet<TOrders> TOrders { get; set; }
    public DbSet<TOrderItems> TOrderItems { get; set; }
    public DbSet<TOrderStatus> TOrderStatus { get; set; }

    #endregion Order

    #region Product

    public DbSet<TProducts> TProducts { get; set; }
    public DbSet<TCategory> TCategory { get; set; }

    #endregion Product

    #region Security

    public DbSet<TRateLimit> TRateLimit { get; set; }

    #endregion

    #region User

    public DbSet<TUserOtp> TUserOtp { get; set; }
    public DbSet<TUsers> TUsers { get; set; }

    #endregion User

    #endregion DbSets

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TRefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(500);
            entity.Property(x => x.CreatedByIp).HasMaxLength(45);
            entity.Property(x => x.UserAgent).HasMaxLength(255);
            entity.HasOne(x => x.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TCarts>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.UserId).IsRequired();
            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.CartItems)
                  .WithOne(ci => ci.Cart)
                  .HasForeignKey(ci => ci.CartId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.UserId)
                  .IsUnique()
                  .HasDatabaseName("IX_Carts_UserId_Unique");
        });

        builder.Entity<TCartItems>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CartId).IsRequired();
            entity.Property(x => x.ProductId).IsRequired();
            entity.Property(x => x.Quantity).IsRequired().HasDefaultValue(1);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne(x => x.Cart)
                  .WithMany(c => c.CartItems)
                  .HasForeignKey(x => x.CartId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                  .WithMany()
                  .HasForeignKey(x => x.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.CartId, x.ProductId, x.Color, x.Size })
                  .IsUnique()
                  .HasDatabaseName("IX_CartItems_CartId_ProductId_Color_Size");
        });

        builder.Entity<TOrders>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.PostalCode).HasMaxLength(20);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(100);
            entity.Property(x => x.TotalAmount).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.TotalProfit).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne(x => x.User)
                  .WithMany(u => u.UserOrders)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OrderStatus)
                  .WithMany(os => os.Orders)
                  .HasForeignKey(x => x.OrderStatusId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.OrderItems)
                  .WithOne(oi => oi.UserOrder)
                  .HasForeignKey(oi => oi.UserOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.UserId, x.IdempotencyKey })
                  .IsUnique()
                  .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        });

        builder.Entity<TOrderItems>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.PurchasePrice).IsRequired();
            entity.Property(x => x.SellingPrice).IsRequired();
            entity.Property(x => x.Quantity).IsRequired();
            entity.Property(x => x.Amount).IsRequired().HasDefaultValue(0);
            entity.Property(x => x.Profit).IsRequired().HasDefaultValue(0);
            entity.HasOne(x => x.UserOrder)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(x => x.UserOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                  .WithMany(p => p.OrderDetails)
                  .HasForeignKey(x => x.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TOrderStatus>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Icon).HasMaxLength(200);
            entity.HasMany(x => x.Orders)
                  .WithOne(o => o.OrderStatus)
                  .HasForeignKey(o => o.OrderStatusId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Seed Data for Order Statuses
            entity.HasData(
                new TOrderStatus { Id = 1, Name = "در انتظار پرداخت", Icon = "hourglass_empty" },
                new TOrderStatus { Id = 2, Name = "در حال پردازش", Icon = "sync" },
                new TOrderStatus { Id = 3, Name = "ارسال شده", Icon = "local_shipping" },
                new TOrderStatus { Id = 4, Name = "تحویل داده شده", Icon = "done_all" },
                new TOrderStatus { Id = 5, Name = "لغو شده", Icon = "cancel" },
                new TOrderStatus { Id = 6, Name = "مرجوعی", Icon = "assignment_return" }
            );
        });

        builder.Entity<TProducts>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Icon).HasMaxLength(500);
            entity.Property(x => x.Count).HasDefaultValue(0);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne(x => x.Category)
                  .WithMany(pt => pt.Products)
                  .HasForeignKey(x => x.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.OrderDetails)
                  .WithOne(od => od.Product)
                  .HasForeignKey(od => od.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TCategory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Icon).HasMaxLength(200);
            entity.HasMany(x => x.Products)
                  .WithOne(p => p.Category)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TRateLimit>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Key).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Count).IsRequired();
            entity.Property(x => x.LastAttempt).IsRequired();
            entity.HasIndex(x => x.Key).IsUnique();
        });

        builder.Entity<TUserOtp>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(o => o.OtpHash).IsRequired().HasMaxLength(500);
            entity.Property(o => o.ExpiresAt).IsRequired();
            entity.Property(o => o.IsUsed).HasDefaultValue(false);
            entity.Property(o => o.AttemptCount).HasDefaultValue(0);
            entity.HasOne(o => o.User)
                  .WithMany(u => u.UserOtps)
                  .HasForeignKey(o => o.UserId)
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
            entity.HasIndex(u => u.PhoneNumber).IsUnique();
            entity.HasMany(x => x.UserOrders)
                  .WithOne(o => o.User)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(u => u.UserOtps)
                  .WithOne(o => o.User)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(u => u.RefreshTokens)
                  .WithOne(rt => rt.User)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}