using Domain.Variant.Entities;
using Domain.Variant.ValueObjects;
using Domain.Shipping.ValueObjects;

namespace Infrastructure.Variant.Configurations;

internal sealed class VariantShippingConfiguration
    : IEntityTypeConfiguration<VariantShipping>
{
    public void Configure(EntityTypeBuilder<VariantShipping> builder)
    {
        builder.HasKey(e => new { e.ProductVariantId, e.ShippingId });

        builder.Property(e => e.ProductVariantId)
            .HasConversion(id => id.Value, value => VariantId.From(value))
            .IsRequired();

        builder.Property(e => e.ShippingId)
            .HasConversion(id => id.Value, value => ShippingId.From(value))
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasOne(e => e.ProductVariant)
            .WithMany(v => v.ProductVariantShippings)
            .HasForeignKey(e => e.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Shipping)
            .WithMany()
            .HasForeignKey(e => e.ShippingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}