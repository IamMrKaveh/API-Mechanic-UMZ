using Domain.Discount.Entities;
using Domain.Discount.Enums;
using Domain.Discount.ValueObjects;

namespace Infrastructure.Discount.Configurations;

public sealed class DiscountRestrictionConfiguration : IEntityTypeConfiguration<DiscountRestriction>
{
    public void Configure(EntityTypeBuilder<DiscountRestriction> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);

        builder.Property(e => e.DiscountCodeId)
            .IsRequired();

        builder.Property(e => e.RestrictionType)
            .HasConversion(new EnumToStringConverter<DiscountRestrictionType>())
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.RestrictionValue)
            .IsRequired()
            .HasMaxLength(500);

        builder.ToTable("DiscountRestrictions");
    }
}
