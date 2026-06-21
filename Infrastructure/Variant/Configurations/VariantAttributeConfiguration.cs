using Domain.Attribute.ValueObjects;
using Domain.Variant.Entities;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Configurations;

internal sealed class VariantAttributeConfiguration
    : IEntityTypeConfiguration<VariantAttribute>
{
    public void Configure(EntityTypeBuilder<VariantAttribute> builder)
    {
        builder.ToTable("ProductVariantAttributes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("Id")
            .HasConversion(v => v.Value, v => VariantAttributeId.From(v))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(e => e.VariantId)
            .HasColumnName("VariantId")
            .HasConversion(id => id.Value, value => VariantId.From(value))
            .IsRequired();

        builder.Property(e => e.AttributeTypeId)
            .HasColumnName("AttributeTypeId")
            .HasConversion(id => id.Value, value => AttributeTypeId.From(value))
            .IsRequired();

        builder.Property(e => e.ValueId)
            .HasColumnName("ValueId")
            .HasConversion(id => id.Value, value => AttributeValueId.From(value))
            .IsRequired();

        builder.Property(e => e.DisplayValue)
            .HasColumnName("DisplayValue")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasOne(e => e.Value)
            .WithMany()
            .HasForeignKey(e => e.ValueId)
            .HasPrincipalKey(v => v.Id)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(e => e.AttributeType)
            .WithMany()
            .HasForeignKey(e => e.AttributeTypeId)
            .HasPrincipalKey(t => t.Id)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.Ignore("AttributeValueId");

        builder.HasIndex(e => new { e.VariantId, e.ValueId })
            .IsUnique()
            .HasDatabaseName("IX_ProductVariantAttributes_Variant_Value");

        builder.HasIndex(e => new { e.VariantId, e.AttributeTypeId })
            .IsUnique()
            .HasDatabaseName("IX_ProductVariantAttributes_Variant_Type");

        builder.HasIndex(e => e.VariantId)
            .HasDatabaseName("IX_ProductVariantAttributes_VariantId");

        builder.HasQueryFilter(e => !e.AttributeType.IsDeleted);
    }
}