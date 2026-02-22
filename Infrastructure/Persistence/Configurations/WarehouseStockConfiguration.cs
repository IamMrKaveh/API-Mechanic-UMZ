namespace Infrastructure.Persistence.Configurations;

public sealed class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(e => e.Variant).WithMany().HasForeignKey(e => e.VariantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.WarehouseId, e.VariantId }).IsUnique();
    }
}