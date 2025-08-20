using DataAccessLayer.Models.Auth;

namespace DataAccessLayer;

public class MechanicContext : DbContext
{
    private readonly string _connectionString =
@"
Data Source =.;
Initial Catalog = DB-Mechanic;
User ID=mrkaveh;
Password=1234;
TrustServerCertificate = True;
";

    #region DbSets

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

    public DbSet<TUsers> TUsers { get; set; }

    #endregion User

    #endregion DbSets

    public MechanicContext(DbContextOptions<MechanicContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_connectionString);
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
            entity.HasOne(x => x.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
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

        builder.Entity<TUsers>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.HasMany(x => x.UserOrders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        #endregion
    }
}
