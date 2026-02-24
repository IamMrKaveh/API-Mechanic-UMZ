namespace Infrastructure.Persistence.Configurations;

public sealed class ProductVariantAttributeConfiguration : IEntityTypeConfiguration<ProductVariantAttribute>
{
    public void Configure(EntityTypeBuilder<ProductVariantAttribute> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(e => e.Variant).WithMany(v => v.VariantAttributes).HasForeignKey(e => e.VariantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.AttributeValue).WithMany(v => v.VariantAttributes).HasForeignKey(e => e.AttributeValueId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.VariantId, e.AttributeValueId }).IsUnique();
    }
}