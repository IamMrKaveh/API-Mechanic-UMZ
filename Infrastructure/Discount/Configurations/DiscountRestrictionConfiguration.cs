using Domain.Discount.Entities;
using Domain.Discount.ValueObjects;

namespace Infrastructure.Discount.Configurations;

public sealed class DiscountRestrictionConfiguration : IEntityTypeConfiguration<DiscountRestriction>
{
    public void Configure(EntityTypeBuilder<DiscountRestriction> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => DiscountRestrictionId.From(v));

        builder.Property(e => e.DiscountCodeId)
            .HasConversion(v => v.Value, v => DiscountCodeId.From(v))
            .IsRequired();

        builder.Property(e => e.RestrictionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.RestrictionValue)
            .IsRequired()
            .HasMaxLength(500);

        builder.ToTable("DiscountRestrictions");
    }
}