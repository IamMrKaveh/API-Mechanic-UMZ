using Domain.Cart.Entities;
using Domain.Cart.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Cart.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => CartItemId.From(v));

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(e => e.CartId)
            .HasConversion(v => v.Value, v => CartId.From(v))
            .IsRequired();

        builder.Property(e => e.VariantId)
            .HasConversion(v => v.Value, v => VariantId.From(v))
            .IsRequired();

        builder.Property(e => e.ProductId)
            .HasConversion(v => v.Value, v => ProductId.From(v))
            .IsRequired();

        builder.OwnsOne(e => e.ProductName, pb =>
        {
            pb.Property(p => p.Value).HasColumnName("ProductName").IsRequired().HasMaxLength(500);
        });

        builder.OwnsOne(e => e.Sku, pb =>
        {
            pb.Property(p => p.Value).HasColumnName("Sku").IsRequired().HasMaxLength(100);
        });

        builder.OwnsOne(e => e.SellingPrice, pb =>
        {
            pb.Property(p => p.Amount).HasColumnName("SellingPrice").IsRequired();
            pb.Property(p => p.Currency).HasColumnName("SellingPriceCurrency").HasMaxLength(10);
        });

        builder.OwnsOne(e => e.OriginalPrice, pb =>
        {
            pb.Property(p => p.Amount).HasColumnName("OriginalPrice").IsRequired();
            pb.Property(p => p.Currency).HasColumnName("OriginalPriceCurrency").HasMaxLength(10);
        });

        builder.Property(e => e.Quantity).IsRequired();
        builder.Property(e => e.AddedAt).IsRequired();

        builder.HasIndex(e => new { e.CartId, e.VariantId }).IsUnique();
    }
}