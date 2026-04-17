using Domain.Discount.Entities;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Discount.Configurations;

public sealed class DiscountUsageConfiguration : IEntityTypeConfiguration<DiscountUsageRecord>
{
    public void Configure(EntityTypeBuilder<DiscountUsageRecord> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => DiscountUsageId.From(v));

        builder.Property(e => e.DiscountCodeId)
            .HasConversion(v => v.Value, v => DiscountCodeId.From(v))
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasConversion(v => v.Value, v => UserId.From(v))
            .IsRequired();

        builder.Property(e => e.OrderId)
            .HasConversion(v => v.Value, v => OrderId.From(v))
            .IsRequired();

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.DiscountedAmount)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(e => e.UsageCountAtTime).IsRequired();
        builder.Property(e => e.UsedAt).IsRequired();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.DiscountCodeId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.OrderId);

        builder.ToTable("DiscountUsageRecords");
    }
}