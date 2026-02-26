namespace Infrastructure.Wallet.Configurations;

public sealed class WalletLedgerEntryConfiguration : IEntityTypeConfiguration<WalletLedgerEntry>
{
    public void Configure(EntityTypeBuilder<WalletLedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property<int>("_walletId")
            .HasColumnName("WalletId")
            .IsRequired();

        builder.Property<int>("_userId")
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property<decimal>("_amountDelta")
            .HasColumnName("AmountDelta")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property<decimal>("_balanceAfter")
            .HasColumnName("BalanceAfter")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property<WalletTransactionType>("_transactionType")
            .HasColumnName("TransactionType")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property<WalletReferenceType>("_referenceType")
            .HasColumnName("ReferenceType")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property<int>("_referenceId")
            .HasColumnName("ReferenceId")
            .IsRequired();

        builder.Property<string>("_idempotencyKey")
            .HasColumnName("IdempotencyKey")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property<string?>("_correlationId")
            .HasColumnName("CorrelationId")
            .HasMaxLength(200);

        builder.Property<string?>("_description")
            .HasColumnName("Description")
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex("_idempotencyKey")
            .IsUnique()
            .HasDatabaseName("IX_WalletLedgerEntries_IdempotencyKey");

        builder.HasIndex("_walletId")
            .HasDatabaseName("IX_WalletLedgerEntries_WalletId");

        builder.HasIndex("_userId")
            .HasDatabaseName("IX_WalletLedgerEntries_UserId");

        builder.HasIndex(new[] { "_walletId", "_referenceType", "_referenceId" })
            .HasDatabaseName("IX_WalletLedgerEntries_WalletId_ReferenceType_ReferenceId");
    }
}