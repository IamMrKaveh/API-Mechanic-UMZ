namespace Infrastructure.Order.Configurations;

public sealed class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
{
    public void Configure(EntityTypeBuilder<OrderStatus> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(50);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Icon).HasMaxLength(100);
        builder.Property(e => e.Color).HasMaxLength(50);

        builder.HasIndex(e => e.Name).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}