namespace Infrastructure.Persistence.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.City).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.Phone).HasMaxLength(20);

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasMany(e => e.Stocks).WithOne(s => s.Warehouse).HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Cascade);
    }
}