using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Configurations;

public sealed class WalletDebitRequestConfiguration : IEntityTypeConfiguration<WalletDebitRequest>
{
    public void Configure(EntityTypeBuilder<WalletDebitRequest> builder)
    {
        builder.ToTable("WalletDebitRequests");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WalletDebitRequestId.From(value));

        builder.Property(e => e.WalletId)
            .HasConversion(id => id.Value, value => WalletId.From(value))
            .IsRequired();

        builder.Property(e => e.OwnerId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.OwnsOne(e => e.Amount, b =>
        {
            b.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            b.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(10).IsRequired();
        });

        builder.Property(e => e.Reason).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);

        builder.Property(e => e.RequestedBy)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(e => e.ReservationId)
            .HasConversion(id => id.Value, value => WalletReservationId.From(value))
            .IsRequired();

        builder.Property(e => e.RespondedBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.Property(e => e.Status).HasConversion<int>().IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.RespondedAt);

        builder.HasIndex(e => new { e.OwnerId, e.Status }).HasDatabaseName("IX_WalletDebitRequests_Owner_Status");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName("IX_WalletDebitRequests_ExpiresAt");
    }
}
