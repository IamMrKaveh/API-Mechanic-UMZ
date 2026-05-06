using Domain.Attribute.Entities;
using Domain.Attribute.ValueObjects;

namespace Infrastructure.Attribute.Configurations;

public sealed class AttributeValueConfiguration : IEntityTypeConfiguration<AttributeValue>
{
    public void Configure(EntityTypeBuilder<AttributeValue> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => AttributeValueId.From(v));

        builder.Property(e => e.AttributeTypeId)
            .HasConversion(v => v.Value, v => AttributeTypeId.From(v))
            .IsRequired();

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(e => e.Value).IsRequired().HasMaxLength(100);
        builder.Property(e => e.DisplayValue).IsRequired().HasMaxLength(100);
        builder.Property(e => e.HexCode).HasMaxLength(50);
        builder.Property(e => e.SortOrder).IsRequired();
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}