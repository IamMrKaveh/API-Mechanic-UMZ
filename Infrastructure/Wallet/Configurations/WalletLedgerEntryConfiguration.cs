using Domain.Wallet.Entities;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Configurations;

public sealed class WalletLedgerEntryConfiguration : IEntityTypeConfiguration<WalletLedgerEntry>
{
    public void Configure(EntityTypeBuilder<WalletLedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WalletLedgerEntryId.From(value));

        builder.Property(e => e.WalletId)
            .HasConversion(id => id.Value, value => WalletId.From(value))
            .HasColumnName("WalletId")
            .IsRequired();

        builder.Property(e => e.OwnerId)
            .HasConversion(id => id.Value, value => Domain.User.ValueObjects.UserId.From(value))
            .HasColumnName("UserId")
            .IsRequired();

        builder.OwnsOne(e => e.Amount, a =>
        {
            a.Property(m => m.Amount).HasColumnName("AmountDelta").HasColumnType("decimal(18,2)").IsRequired();
            a.Property(m => m.Currency).HasColumnName("AmountCurrency").HasMaxLength(10).IsRequired();
        });

        builder.OwnsOne(e => e.BalanceAfter, b =>
        {
            b.Property(m => m.Amount).HasColumnName("BalanceAfter").HasColumnType("decimal(18,2)").IsRequired();
            b.Property(m => m.Currency).HasColumnName("BalanceAfterCurrency").HasMaxLength(10).IsRequired();
        });

        builder.Property(e => e.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ReferenceId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.IdempotencyKey).HasMaxLength(200);
        builder.Property(e => e.OccurredAt).HasColumnName("CreatedAt").IsRequired();

        builder.HasIndex(e => e.WalletId).HasDatabaseName("IX_WalletLedgerEntries_WalletId");
        builder.HasIndex(e => e.OwnerId).HasDatabaseName("IX_WalletLedgerEntries_UserId");
        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL")
            .HasDatabaseName("IX_WalletLedgerEntries_IdempotencyKey");
    }
}