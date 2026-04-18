using Domain.Wallet.Entities;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Configurations;

public sealed class WalletReservationConfiguration : IEntityTypeConfiguration<WalletReservation>
{
    public void Configure(EntityTypeBuilder<WalletReservation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WalletReservationId.From(value));

        builder.Property("WalletId")
            .HasColumnName("WalletId")
            .IsRequired();

        builder.OwnsOne(e => e.Amount, a =>
        {
            a.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            a.Property(m => m.Currency).HasColumnName("AmountCurrency").HasMaxLength(10).IsRequired();
        });

        builder.Property(e => e.Purpose).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ExpiresAt);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.ResolvedAt);

        builder.HasOne(e => e.Wallet)
            .WithMany(w => w.ActiveReservations)
            .HasForeignKey("WalletId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex("WalletId").HasDatabaseName("IX_WalletReservations_WalletId");
        builder.HasIndex(new[] { "WalletId", "Status" })
            .HasDatabaseName("IX_WalletReservations_WalletId_Status");
    }
}