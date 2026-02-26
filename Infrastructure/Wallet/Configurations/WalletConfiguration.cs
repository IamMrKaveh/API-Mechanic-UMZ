namespace Infrastructure.Wallet.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Domain.Wallet.Wallet>
{
    public void Configure(EntityTypeBuilder<Domain.Wallet.Wallet> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.Property<int>("_userId")
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property<decimal>("_currentBalance")
            .HasColumnName("CurrentBalance")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property<decimal>("_reservedBalance")
            .HasColumnName("ReservedBalance")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property<WalletStatus>("_status")
            .HasColumnName("Status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex("_userId")
            .IsUnique()
            .HasDatabaseName("IX_Wallets_UserId");

        builder.HasMany(e => e.Reservations)
            .WithOne(r => r.Wallet)
            .HasForeignKey("_walletId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Domain.Wallet.Wallet.Reservations))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata
            .FindNavigation(nameof(Domain.Wallet.Wallet.PendingLedgerEntries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}