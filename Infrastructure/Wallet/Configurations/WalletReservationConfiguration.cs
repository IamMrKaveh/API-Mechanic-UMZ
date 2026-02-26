namespace Infrastructure.Wallet.Configurations;

public sealed class WalletReservationConfiguration : IEntityTypeConfiguration<WalletReservation>
{
    public void Configure(EntityTypeBuilder<WalletReservation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property<int>("_walletId")
            .HasColumnName("WalletId")
            .IsRequired();

        builder.Property<decimal>("_amount")
            .HasColumnName("Amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property<int>("_orderId")
            .HasColumnName("OrderId")
            .IsRequired();

        builder.Property<WalletReservationStatus>("_status")
            .HasColumnName("Status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property<DateTime?>("_expiresAt")
            .HasColumnName("ExpiresAt");

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.Wallet)
            .WithMany(w => w.Reservations)
            .HasForeignKey("_walletId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex("_walletId")
            .HasDatabaseName("IX_WalletReservations_WalletId");

        builder.HasIndex("_orderId")
            .HasDatabaseName("IX_WalletReservations_OrderId");

        builder.HasIndex(new[] { "_walletId", "_orderId", "_status" })
            .HasDatabaseName("IX_WalletReservations_WalletId_OrderId_Status");

        builder.HasIndex("_expiresAt")
            .HasDatabaseName("IX_WalletReservations_ExpiresAt")
            .HasFilter("\"Status\" = 'Pending'");
    }
}