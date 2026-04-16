using Domain.Order.Entities;
using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Order.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasConversion(v => v.Value, v => OrderItemId.From(v));

        builder.Property(e => e.OrderId)
               .HasConversion(v => v.Value, v => OrderId.From(v))
               .IsRequired();

        builder.Property(e => e.VariantId)
               .HasConversion(v => v.Value, v => VariantId.From(v))
               .IsRequired();

        builder.Property(e => e.ProductId)
               .HasConversion(v => v.Value, v => ProductId.From(v))
               .IsRequired();

        builder.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Sku).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Quantity).IsRequired();

        builder.OwnsOne(e => e.UnitPrice, mb =>
        {
            mb.Property(m => m.Amount)
              .HasColumnName("UnitPriceAmount")
              .HasColumnType("decimal(18,2)")
              .IsRequired();
            mb.Property(m => m.Currency)
              .HasColumnName("UnitPriceCurrency")
              .HasMaxLength(5);
        });

        builder.Ignore(e => e.TotalPrice);
    }
}