using Domain.Discount.Aggregates;
using Domain.Discount.ValueObjects;

namespace Infrastructure.Discount.Configurations;

public sealed class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => DiscountCodeId.From(v));

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.OwnsOne(e => e.Value, v =>
        {
            v.Property(x => x.Amount)
                .HasColumnName("DiscountValue")
                .HasColumnType("decimal(18,4)")
                .IsRequired();

            v.Property(x => x.Type)
                .HasColumnName("DiscountType")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
        });

        builder.Property(e => e.MaximumDiscountAmount)
            .HasConversion(
                v => v == null ? (decimal?)null : v.Amount,
                v => v == null ? null : Money.Create(v.Value, "IRT"))
            .HasColumnName("MaximumDiscountAmount")
            .HasColumnType("decimal(18,4)");

        builder.Property(e => e.UsageLimit);
        builder.Property(e => e.UsageCount).IsRequired();
        builder.Property(e => e.StartsAt);
        builder.Property(e => e.ExpiresAt);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasMany(e => e.Restrictions)
            .WithOne(r => r.DiscountCode)
            .HasForeignKey(r => r.DiscountCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Usages)
            .WithOne()
            .HasForeignKey(u => u.DiscountCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.ToTable("DiscountCodes");
    }
}