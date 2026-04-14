using Domain.Attribute.ValueObjects;
using Domain.Variant.Entities;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Configurations;

internal sealed class VariantAttributeConfiguration
    : IEntityTypeConfiguration<ProductVariantAttribute>
{
    public void Configure(EntityTypeBuilder<ProductVariantAttribute> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();

        builder.Property(e => e.VariantId)
            .HasConversion(id => id.Value, value => VariantId.From(value))
            .IsRequired();

        builder.Property(e => e.AttributeValueId)
            .HasConversion(id => id.Value, value => AttributeValueId.From(value))
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(new[] { "VariantId", "AttributeValueId" }).IsUnique();

        builder.HasOne(e => e.AttributeValue)
            .WithMany()
            .HasForeignKey(e => e.AttributeValueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}