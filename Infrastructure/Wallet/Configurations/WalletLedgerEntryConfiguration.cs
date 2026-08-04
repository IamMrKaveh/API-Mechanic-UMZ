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
            a.Property(m => m.Amount)
                .HasColumnName("AmountDelta")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            a.Property(m => m.Currency)
                .HasColumnName("AmountCurrency")
                .HasMaxLength(10)
                .IsRequired();
        });

        builder.OwnsOne(e => e.BalanceAfter, b =>
        {
            b.Property(m => m.Amount)
                .HasColumnName("BalanceAfter")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            b.Property(m => m.Currency)
                .HasColumnName("BalanceAfterCurrency")
                .HasMaxLength(10)
                .IsRequired();
        });

        builder.Property(e => e.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ReferenceId).IsRequired().HasMaxLength(200);
        builder.Property(e => e.IdempotencyKey).HasMaxLength(200);
        builder.Property(e => e.CorrelationId).HasMaxLength(128);
        builder.Property(e => e.OccurredAt).HasColumnName("CreatedAt").IsRequired();

        builder.Property(e => e.DebitRequestId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? WalletDebitRequestId.From(value.Value) : null)
            .HasColumnName("DebitRequestId");

        builder.Property(e => e.WithdrawalRequestId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? WalletWithdrawalRequestId.From(value.Value) : null)
            .HasColumnName("WithdrawalRequestId");

        builder.Property(e => e.TransferId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? WalletTransferId.From(value.Value) : null)
            .HasColumnName("TransferId");

        builder.Property(e => e.TopUpId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? WalletTopUpId.From(value.Value) : null)
            .HasColumnName("TopUpId");

        builder.HasOne(e => e.Wallet)
            .WithMany()
            .HasForeignKey(e => e.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Owner)
            .WithMany()
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WalletDebitRequest>()
            .WithMany()
            .HasForeignKey(e => e.DebitRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Domain.Wallet.Aggregates.WalletWithdrawalRequest>()
            .WithMany()
            .HasForeignKey(e => e.WithdrawalRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Domain.Wallet.Aggregates.WalletTransfer>()
            .WithMany()
            .HasForeignKey(e => e.TransferId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Domain.Wallet.Aggregates.WalletTopUp>()
            .WithMany()
            .HasForeignKey(e => e.TopUpId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.WalletId, e.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_WalletLedgerEntries_WalletId_OccurredAt");

        builder.HasIndex(e => new { e.OwnerId, e.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_WalletLedgerEntries_UserId_OccurredAt");

        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL")
            .HasDatabaseName("IX_WalletLedgerEntries_IdempotencyKey");

        builder.HasIndex(e => e.CorrelationId)
            .HasFilter("\"CorrelationId\" IS NOT NULL")
            .HasDatabaseName("IX_WalletLedgerEntries_CorrelationId");

        builder.HasIndex(e => e.DebitRequestId)
            .HasFilter("\"DebitRequestId\" IS NOT NULL")
            .HasDatabaseName("IX_WalletLedgerEntries_DebitRequestId");

        builder.HasIndex(e => e.WithdrawalRequestId)
            .HasFilter("\"WithdrawalRequestId\" IS NOT NULL")
            .HasDatabaseName("IX_WalletLedgerEntries_WithdrawalRequestId");

        builder.HasIndex(e => e.TransferId)
            .HasFilter("\"TransferId\" IS NOT NULL")
            .HasDatabaseName("IX_WalletLedgerEntries_TransferId");

        builder.HasIndex(e => e.TopUpId)
            .HasFilter("\"TopUpId\" IS NOT NULL")
            .HasDatabaseName("IX_WalletLedgerEntries_TopUpId");

        builder.HasQueryFilter(e => e.Owner.IsActive);
    }
}
