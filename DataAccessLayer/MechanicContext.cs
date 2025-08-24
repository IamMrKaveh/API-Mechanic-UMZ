namespace DataAccessLayer;

public class MechanicContext : DbContext
{
    // for migration (Supabase direct)
    private readonly string _directConnectionString =
    @"
Host=db.suldeexbzqpfiflqzkej.supabase.co;
Port=5432;
Database=postgres;
Username=postgres;
Password=Breaking355662Bad!;
SSL Mode=Require;
Trust Server Certificate=true;
";

    // local connection (SQL Server on local machine)
    private readonly string _connectionString =
    @"
Server=.;
Database=DB-Mechanic;
User Id=mrkaveh;
Password=1234;
TrustServerCertificate=True;
";

    // for Worker (Supabase connection pooler, recommended for production)
    private readonly string _poolerConnectionString =
    @"
Host=aws-1-eu-north-1.pooler.supabase.com;
Port=5432;
Database=postgres;
Username=postgres.suldeexbzqpfiflqzkej;
Password=Breaking355662Bad!;
SSL Mode=Require;
Trust Server Certificate=true;
";

    // for API (Supabase transaction pooler)
    private readonly string _transactionConnection =
    @"
Host=aws-1-eu-north-1.pooler.supabase.com;
Port=6543;
Database=postgres;
Username=postgres.suldeexbzqpfiflqzkej;
Password=Breaking355662Bad!;
SSL Mode=Require;
Trust Server Certificate=true;
";


    #region DbSets

    #region Auth

    public DbSet<TRefreshToken> TRefreshToken { get; set; }

    #endregion

    #region Cart

    public DbSet<TCarts> TCarts { get; set; }
    public DbSet<TCartItems> TCartItems { get; set; }

    #endregion

    #region Order

    public DbSet<TOrders> TOrders { get; set; }
    public DbSet<TOrderItems> TOrderItems { get; set; }
    public DbSet<TOrderStatus> TOrderStatus { get; set; }

    #endregion Order

    #region Product

    public DbSet<TProducts> TProducts { get; set; }
    public DbSet<TProductTypes> TProductTypes { get; set; }

    #endregion Product

    #region User

    public DbSet<TUserOtp> TUserOtps { get; set; }
    public DbSet<TUsers> TUsers { get; set; }

    #endregion User

    #endregion DbSets

    public MechanicContext(DbContextOptions<MechanicContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_transactionConnection);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        #region Cart

        builder.Entity<TCarts>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.UserId)
                .IsRequired();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(x => x.CartItems)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TCartItems>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.CartId)
                .IsRequired();

            entity.Property(x => x.ProductId)
                .IsRequired();

            entity.Property(x => x.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            entity.HasOne(x => x.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(x => new { x.CartId, x.ProductId })
                .IsUnique()
                .HasDatabaseName("IX_CartItems_CartId_ProductId");
        });

        #endregion

        #region Order

        builder.Entity<TOrders>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.HasOne(x => x.User)
                .WithMany(u => u.UserOrders)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.OrderStatus)
                .WithMany(os => os.Orders)
                .HasForeignKey(x => x.OrderStatusId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(x => x.OrderItems)
                .WithOne(oi => oi.UserOrder)
                .HasForeignKey(oi => oi.UserOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TOrderItems>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.HasOne(x => x.UserOrder)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(x => x.UserOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<TOrderStatus>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.HasMany(x => x.Orders)
                .WithOne(o => o.OrderStatus)
                .HasForeignKey(o => o.OrderStatusId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        #endregion

        #region Product

        builder.Entity<TProducts>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.HasOne(x => x.ProductType)
                .WithMany(pt => pt.Products)
                .HasForeignKey(x => x.ProductTypeId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(x => x.OrderDetails)
                .WithOne(od => od.Product)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<TProductTypes>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.HasMany(x => x.Products)
                .WithOne(p => p.ProductType)
                .HasForeignKey(p => p.ProductTypeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        #endregion

        #region User

        builder.Entity<TUserOtp>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
            .ValueGeneratedOnAdd();

            entity.Property(o => o.OtpHash).IsRequired();

            entity.Property(o => o.CreatedAt)
            .HasDefaultValueSql("TIMEZONE('UTC', NOW())");

            entity.Property(o => o.IsUsed)
            .HasDefaultValue(false);
        });

        builder.Entity<TUsers>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.HasIndex(u => u.PhoneNumber)
            .IsUnique();

            entity.HasMany(x => x.UserOrders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(u => u.UserOtps)
              .WithOne(o => o.User)
              .HasForeignKey(o => o.UserId)
              .OnDelete(DeleteBehavior.Cascade);
        });

        #endregion
    }
}