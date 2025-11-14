namespace Infrastructure.Persistence;

public class MechanicContext : DbContext
{
    public MechanicContext(DbContextOptions<MechanicContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(MechanicContext).Assembly);

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
    }
}