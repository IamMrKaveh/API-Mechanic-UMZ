using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => PaymentTransactionId.From(v));

        builder.Property(e => e.Authority)
            .HasConversion(v => v.Value, v => PaymentAuthority.Create(v))
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Amount)
            .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRT"))
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.Gateway)
            .HasConversion(v => v.Value, v => PaymentGateway.FromString(v))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion(
                v => v.Value,
                v => Domain.Payment.ValueObjects.PaymentStatus.FromString(v))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.OrderId)
            .HasConversion(v => v.Value, v => OrderId.From(v))
            .IsRequired();

        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Order)
            .WithOne(e => e.PaymentTransaction)
            .HasForeignKey<PaymentTransaction>(e => e.OrderId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Authority).IsUnique();
        builder.HasIndex(e => e.OrderId).IsUnique();
        builder.HasIndex(e => new { e.Status, e.CreatedAt });
        builder.HasIndex(e => e.UserId);

        builder.HasQueryFilter(e => !e.Order.IsDeleted);
    }
}