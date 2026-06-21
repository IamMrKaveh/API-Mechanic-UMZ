using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Configurations;

internal sealed class VariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => VariantId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.ProductId)
            .HasConversion(id => id.Value, value => ProductId.From(value))
            .IsRequired();

        builder.Property(e => e.Sku)
            .HasConversion(s => s.Value, v => Sku.Create(v))
            .IsRequired()
            .HasMaxLength(100);

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

        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.Sku).IsUnique();
        builder.HasIndex(e => e.ProductId);
        builder.HasIndex(e => e.IsActive);

        builder.HasMany(e => e.Attributes)
            .WithOne(a => a.Variant)
            .HasForeignKey(a => a.VariantId)
            .HasPrincipalKey(v => v.Id)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasMany(e => e.Shippings)
            .WithOne(s => s.Variant)
            .HasForeignKey(s => s.VariantId)
            .HasPrincipalKey(v => v.Id)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        var attributesNav = builder.Metadata.FindNavigation(nameof(ProductVariant.Attributes))!;
        attributesNav.SetPropertyAccessMode(PropertyAccessMode.Field);
        attributesNav.SetField("_attributes");

        var shippingsNav = builder.Metadata.FindNavigation(nameof(ProductVariant.Shippings))!;
        shippingsNav.SetPropertyAccessMode(PropertyAccessMode.Field);
        shippingsNav.SetField("_shippings");
    }
}