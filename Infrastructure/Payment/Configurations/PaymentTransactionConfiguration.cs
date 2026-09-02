using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Payment.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => PaymentTransactionId.From(v));

        builder.Property(e => e.OrderId)
            .HasConversion(v => v.Value, v => OrderId.From(v))
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasConversion(v => v.Value, v => UserId.From(v))
            .IsRequired();

        builder.Property(e => e.Authority)
            .HasConversion(v => v.Value, v => PaymentAuthority.Create(v))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Gateway)
            .HasConversion(v => v.Value, v => PaymentGateway.FromString(v))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion(v => v.Value, v => PaymentStatus.FromString(v))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasConversion(v => v.Amount, v => Money.FromDecimal(v))
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.Fee)
            .HasConversion(new ValueConverter<decimal, decimal>(v => v, v => v))
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.RefId);

        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.Property(e => e.IsVerificationInProgress).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.VerifiedAt);

        builder.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Authority).IsUnique();
        builder.HasIndex(e => e.OrderId);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.Status, e.CreatedAt });

        builder.HasQueryFilter(e => !e.Order.IsDeleted);
    }
}
