namespace Infrastructure.Persistence.Configurations;

public sealed class ProductVariantShippingConfiguration : IEntityTypeConfiguration<ProductVariantShipping>
{
    public void Configure(EntityTypeBuilder<ProductVariantShipping> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(e => e.ProductVariant).WithMany(v => v.ProductVariantShippings).HasForeignKey(e => e.ProductVariantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Shipping).WithMany(s => s.ProductVariantShippings).HasForeignKey(e => e.ShippingId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ProductVariantId, e.ShippingId }).IsUnique();
    }
}