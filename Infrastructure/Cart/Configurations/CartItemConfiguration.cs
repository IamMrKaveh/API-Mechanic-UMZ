using Domain.Cart.Entities;
using Domain.Cart.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Cart.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(v => v.Value, v => CartItemId.From(v))
            .ValueGeneratedNever();

        builder.Property(x => x.VariantId)
            .HasConversion(v => v.Value, v => VariantId.From(v))
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasConversion(v => v.Value, v => ProductId.From(v))
            .IsRequired();

        builder.Property(x => x.ProductName)
            .HasConversion(v => v.Value, v => ProductName.Create(v))
            .HasMaxLength(ProductName.MaxLength)
            .IsRequired();

        builder.Property(x => x.VariantSku)
            .HasConversion(v => v.Value, v => Sku.Create(v))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.AddedAt).IsRequired();

        builder.OwnsOne(x => x.OriginalPrice, price =>
        {
            price.Property(p => p.Amount)
                 .HasColumnName("OriginalPriceAmount")
                 .HasPrecision(18, 2)
                 .IsRequired();

            price.Property(p => p.Currency)
                 .HasColumnName("OriginalPriceCurrency")
                 .HasMaxLength(3)
                 .IsRequired();

            price.WithOwner();
        });

        builder.OwnsOne(x => x.SellingPrice, price =>
        {
            price.Property(p => p.Amount)
                 .HasColumnName("SellingPriceAmount")
                 .HasPrecision(18, 2)
                 .IsRequired();

            price.Property(p => p.Currency)
                 .HasColumnName("SellingPriceCurrency")
                 .HasMaxLength(3)
                 .IsRequired();

            price.WithOwner();
        });

        builder.Navigation(x => x.SellingPrice).IsRequired();
        builder.Navigation(x => x.OriginalPrice).IsRequired();

        builder.Ignore(x => x.TotalPrice);
        builder.Ignore(x => x.Variant);
        builder.Ignore(x => x.Product);

        builder.HasIndex(x => x.CartId);
        builder.HasIndex(x => x.VariantId);
        builder.HasIndex(x => x.ProductId);
    }
}
