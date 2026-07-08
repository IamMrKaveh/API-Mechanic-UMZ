using Domain.Cart.Entities;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Cart.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductName)
            .HasConversion(v => v.Value, v => ProductName.Create(v))
            .HasMaxLength(ProductName.MaxLength)
            .IsRequired();

        builder.Property(x => x.Sku)
            .HasConversion(v => v.Value, v => Sku.Create(v))
            .HasMaxLength(100)
            .IsRequired();

        builder.OwnsOne(x => x.OriginalPrice, price =>
        {
            price.Property(p => p.Amount).HasColumnName("OriginalPriceAmount").HasPrecision(18, 2);
            price.Property(p => p.Currency).HasColumnName("OriginalPriceCurrency").HasMaxLength(3);
            price.WithOwner();
        });

        builder.OwnsOne(x => x.SellingPrice, price =>
        {
            price.Property(p => p.Amount).HasColumnName("SellingPriceAmount").HasPrecision(18, 2);
            price.Property(p => p.Currency).HasColumnName("SellingPriceCurrency").HasMaxLength(3);
            price.WithOwner();
        });

        builder.HasOne(x => x.Variant)
            .WithMany()
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}