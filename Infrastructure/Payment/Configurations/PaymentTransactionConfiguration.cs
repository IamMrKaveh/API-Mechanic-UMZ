namespace Infrastructure.Payment.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.Property(e => e.Authority)
            .HasConversion(v => v.Value, v => PaymentAuthority.Create(v))
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Amount)
            .HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR"))
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.Gateway)
            .HasConversion(v => v.Value, v => PaymentGateway.FromString(v))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion(v => v.Value, v => Domain.Payment.ValueObjects.PaymentStatus.FromString(v))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.CardPan).HasMaxLength(20);
        builder.Property(e => e.CardHash).HasMaxLength(100);
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.Authority).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasOne(e => e.Order)
            .WithMany(o => o.PaymentTransactions)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}