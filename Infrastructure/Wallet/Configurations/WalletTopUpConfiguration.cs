using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Configurations;

public sealed class WalletTopUpConfiguration : IEntityTypeConfiguration<WalletTopUp>
{
    public void Configure(EntityTypeBuilder<WalletTopUp> builder)
    {
        builder.ToTable("WalletTopUps");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WalletTopUpId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("UserId")
            .IsRequired();

        builder.OwnsOne(e => e.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            m.Property(x => x.Currency).HasColumnName("AmountCurrency").HasMaxLength(10).IsRequired();
        });

        builder.Property(e => e.Gateway)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.GatewayAuthority)
            .HasMaxLength(200);

        builder.Property(e => e.GatewayRefId)
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CompletedAt);
        builder.Property(e => e.FailureReason).HasMaxLength(500);

        builder.Property(e => e.Version)
            .IsConcurrencyToken();

        builder.HasIndex(e => e.GatewayAuthority)
            .IsUnique()
            .HasFilter("\"GatewayAuthority\" IS NOT NULL")
            .HasDatabaseName("IX_WalletTopUps_GatewayAuthority");

        builder.HasIndex(e => e.UserId).HasDatabaseName("IX_WalletTopUps_UserId");
        builder.HasIndex(e => e.Status).HasDatabaseName("IX_WalletTopUps_Status");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_WalletTopUps_CreatedAt");

        builder.Ignore(e => e.DomainEvents);
    }
}