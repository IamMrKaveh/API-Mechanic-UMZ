using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Wallet.Configurations;

public sealed class WalletTransferConfiguration : IEntityTypeConfiguration<WalletTransfer>
{
    public void Configure(EntityTypeBuilder<WalletTransfer> builder)
    {
        builder.ToTable("WalletTransfers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WalletTransferId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.FromUserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("FromUserId")
            .IsRequired();

        builder.Property(e => e.ToUserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("ToUserId")
            .IsRequired();

        builder.OwnsOne(e => e.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            m.Property(x => x.Currency).HasColumnName("AmountCurrency").HasMaxLength(10).IsRequired();
        });

        builder.Property(e => e.Description).HasMaxLength(500);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.OtpHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.OtpExpiresAt).IsRequired();
        builder.Property(e => e.OtpAttempts).IsRequired();

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CompletedAt);
        builder.Property(e => e.CancelledAt);
        builder.Property(e => e.FailureReason).HasMaxLength(500);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => e.FromUserId).HasDatabaseName("IX_WalletTransfers_FromUserId");
        builder.HasIndex(e => e.ToUserId).HasDatabaseName("IX_WalletTransfers_ToUserId");
        builder.HasIndex(e => e.Status).HasDatabaseName("IX_WalletTransfers_Status");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_WalletTransfers_CreatedAt");
        builder.HasIndex(e => e.CorrelationId)
            .IsUnique()
            .HasDatabaseName("IX_WalletTransfers_CorrelationId");

        builder.Ignore(e => e.DomainEvents);
    }
}