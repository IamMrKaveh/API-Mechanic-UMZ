using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Configurations;

internal sealed class VariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => VariantId.From(value));

        builder.Property(e => e.ProductId)
            .HasConversion(id => id.Value, value => ProductId.From(value))
            .IsRequired();

        builder.Property(e => e.Sku)
            .HasConversion(s => s.Value, v => Sku.Create(v))
            .IsRequired()
            .HasMaxLength(100);

        builder.OwnsOne(e => e.PurchasePrice, pp =>
        {
            pp.Property(m => m.Amount).HasColumnName("PurchasePrice").HasColumnType("decimal(18,2)").IsRequired();
            pp.Property(m => m.Currency).HasColumnName("PurchasePriceCurrency").HasMaxLength(10).IsRequired();
        });

        builder.OwnsOne(e => e.SellingPrice, sp =>
        {
            sp.Property(m => m.Amount).HasColumnName("SellingPrice").HasColumnType("decimal(18,2)").IsRequired();
            sp.Property(m => m.Currency).HasColumnName("SellingPriceCurrency").HasMaxLength(10).IsRequired();
        });

        builder.OwnsOne(e => e.OriginalPrice, op =>
        {
            op.Property(m => m.Amount).HasColumnName("OriginalPrice").HasColumnType("decimal(18,2)").IsRequired();
            op.Property(m => m.Currency).HasColumnName("OriginalPriceCurrency").HasMaxLength(10).IsRequired();
        });

        builder.Property(e => e.StockQuantity).IsRequired();
        builder.Property(e => e.IsUnlimited).IsRequired();
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.Sku).IsUnique();
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.IsActive);
    }
}