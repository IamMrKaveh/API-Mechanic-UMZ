using Domain.Order.Entities;
using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Order.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.VariantId)
            .IsRequired();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.Quantity).IsRequired();

        builder.OwnsOne(x => x.UnitPrice, price =>
        {
            price.Property(p => p.Amount)
                 .HasColumnName("UnitPriceAmount")
                 .HasPrecision(18, 2)
                 .IsRequired();

            price.Property(p => p.Currency)
                 .HasColumnName("UnitPriceCurrency")
                 .HasMaxLength(3)
                 .IsRequired();

            price.WithOwner();
        });

        builder.Ignore(x => x.TotalPrice);

        builder.Property(x => x.ProductName)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(x => x.Sku)
               .HasMaxLength(100);

        builder.Navigation(x => x.UnitPrice).IsRequired();

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.VariantId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => new { x.OrderId, x.ProductId });

        builder.HasQueryFilter(oi => !oi.Order.IsDeleted);
    }
}
