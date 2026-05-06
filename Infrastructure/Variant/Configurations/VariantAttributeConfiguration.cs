using Domain.Attribute.ValueObjects;
using Domain.Variant.Entities;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Configurations;

internal sealed class VariantAttributeConfiguration
    : IEntityTypeConfiguration<VariantAttribute>
{
    public void Configure(EntityTypeBuilder<VariantAttribute> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => VariantAttributeId.From(v))
            .IsRequired();

        builder.Property(e => e.VariantId)
            .HasConversion(id => id.Value, value => VariantId.From(value))
            .IsRequired();

        builder.Property(e => e.AttributeTypeId)
            .HasConversion(id => id.Value, value => AttributeTypeId.From(value))
            .IsRequired();

        builder.Property(e => e.ValueId)
            .HasConversion(id => id.Value, value => AttributeValueId.From(value))
            .IsRequired();

        builder.HasIndex(e => new { e.VariantId, e.ValueId }).IsUnique();

        builder.HasOne(e => e.Value)
            .WithMany()
            .HasForeignKey(e => e.ValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AttributeType)
            .WithMany()
            .HasForeignKey(e => e.AttributeTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}