using Domain.Attribute.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Attribute.Configurations;

public sealed class AttributeTypeConfiguration : IEntityTypeConfiguration<AttributeType>
{
    public void Configure(EntityTypeBuilder<AttributeType> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.SortOrder).IsRequired();
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy);

        builder.HasMany(e => e.Values)
            .WithOne(v => v.AttributeType)
            .HasForeignKey(v => v.AttributeTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}