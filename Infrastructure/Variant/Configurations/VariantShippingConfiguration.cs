using Domain.Shipping.ValueObjects;
using Domain.Variant.Entities;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Configurations;

internal sealed class VariantShippingConfiguration : IEntityTypeConfiguration<VariantShipping>
{
    public void Configure(EntityTypeBuilder<VariantShipping> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => VariantShippingId.From(value));

        builder.Property(e => e.VariantId)
            .HasConversion(id => id.Value, value => VariantId.From(value))
            .IsRequired();

        builder.Property(e => e.ShippingId)
            .HasConversion(id => id.Value, value => ShippingId.From(value))
            .IsRequired();

        builder.Property(e => e.Weight).HasColumnType("decimal(10,3)").IsRequired();
        builder.Property(e => e.Width).HasColumnType("decimal(10,3)").IsRequired();
        builder.Property(e => e.Height).HasColumnType("decimal(10,3)").IsRequired();
        builder.Property(e => e.Length).HasColumnType("decimal(10,3)").IsRequired();

        builder.HasOne(e => e.Shipping)
            .WithMany()
            .HasForeignKey(e => e.ShippingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.VariantId);
        builder.HasIndex(e => e.ShippingId);
    }
}