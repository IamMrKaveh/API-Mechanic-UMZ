using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Configurations;

internal sealed class WalletFraudAlertConfiguration : IEntityTypeConfiguration<WalletFraudAlert>
{
    public void Configure(EntityTypeBuilder<WalletFraudAlert> builder)
    {
        builder.ToTable("WalletFraudAlerts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WalletFraudAlertId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.WalletId)
            .HasConversion(id => id.Value, value => WalletId.From(value))
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(e => e.RuleName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.Severity)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(e => e.Metadata)
            .HasMaxLength(2000);

        builder.Property(e => e.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.TriggeredAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.Property(e => e.ReviewedBy)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : UserId.From(value.Value));

        builder.Property(e => e.ReviewedAt);
        builder.Property(e => e.ReviewNote).HasMaxLength(500);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => e.Status).HasDatabaseName("IX_WalletFraudAlerts_Status");
        builder.HasIndex(e => e.Severity).HasDatabaseName("IX_WalletFraudAlerts_Severity");
        builder.HasIndex(e => e.UserId).HasDatabaseName("IX_WalletFraudAlerts_UserId");
        builder.HasIndex(e => e.WalletId).HasDatabaseName("IX_WalletFraudAlerts_WalletId");
        builder.HasIndex(e => e.TriggeredAt).HasDatabaseName("IX_WalletFraudAlerts_TriggeredAt");
        builder.HasIndex(e => new { e.WalletId, e.RuleName, e.TriggeredAt })
            .HasDatabaseName("IX_WalletFraudAlerts_Wallet_Rule_Time");

        builder.Ignore(e => e.DomainEvents);
    }
}