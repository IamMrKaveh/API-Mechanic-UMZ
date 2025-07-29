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

    #region Order

    public DbSet<TOrders> TOrders { get; set; }

    public DbSet<TOrderDetails> TOrderDetails
    { get; set; }

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

        #region Order

        builder.Entity<TOrders>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
            .ValueGeneratedOnAdd();

            entity.HasOne(x => x.OrderDetail)
                .WithOne(xd => xd.UserOrder)
                .HasForeignKey<TOrderDetails>(xd => xd.UserOrderId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.User)
                .WithMany(c => c.UserOrders)
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<TOrderDetails>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
            .ValueGeneratedOnAdd();

            entity.HasMany(x => x.Products)
                .WithMany(p => p.OrderDetails);
        });


        #endregion Order

        #region Product

        builder.Entity<TProducts>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
            .ValueGeneratedOnAdd();

            entity.HasOne(p => p.ProductType)
            .WithMany(pt => pt.Products)
            .HasForeignKey(x => x.ProductTypeId)
            .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(p => p.OrderDetails)
            .WithMany(p => p.Products);
        });

        builder.Entity<TProductTypes>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
            .ValueGeneratedOnAdd();

            entity.HasMany(s => s.Products)
            .WithOne(s => s.ProductType)
            .HasForeignKey(x => x.ProductTypeId)
            .OnDelete(DeleteBehavior.NoAction);
        });

        #endregion Product

        #region User

        builder.Entity<TUsers>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
            .ValueGeneratedOnAdd();

            entity.HasMany(c => c.UserOrders)
                .WithOne(c => c.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });


        #endregion User
    }
}
