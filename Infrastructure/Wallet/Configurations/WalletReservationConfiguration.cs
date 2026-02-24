namespace Infrastructure.Wallet.Configurations;

public sealed class WalletReservationConfiguration : IEntityTypeConfiguration<WalletReservation>
{
    public void Configure(EntityTypeBuilder<WalletReservation> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.Property(r => r.Amount).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(r => new { r.WalletId, r.Status });
        builder.HasIndex(r => r.OrderId);
    }
}