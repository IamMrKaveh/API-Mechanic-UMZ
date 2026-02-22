namespace Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Sku).HasConversion(v => v != null ? v.Value : null, v => v != null ? Sku.Create(v) : null).HasMaxLength(100);
        builder.Property(e => e.RowVersion).IsRowVersion();

        // explicit decimal literal
        builder.Property(e => e.ShippingMultiplier).HasPrecision(18, 2).HasDefaultValue(1.0m);
        builder.HasIndex(e => new { e.ProductId, e.Sku }).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}