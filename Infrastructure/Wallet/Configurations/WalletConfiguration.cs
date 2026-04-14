using Domain.Wallet.Enums;

namespace Infrastructure.Wallet.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Domain.Wallet.Aggregates.Wallet>
{
    public void Configure(EntityTypeBuilder<Domain.Wallet.Aggregates.Wallet> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => Domain.Wallet.ValueObjects.WalletId.From(value));

        builder.Property(e => e.OwnerId)
            .HasConversion(
                id => id.Value,
                value => Domain.User.ValueObjects.UserId.From(value))
            .HasColumnName("UserId")
            .IsRequired();

        builder.OwnsOne(e => e.Balance, b =>
        {
            b.Property(m => m.Amount).HasColumnName("CurrentBalance").HasColumnType("decimal(18,2)").IsRequired();
            b.Property(m => m.Currency).HasColumnName("BalanceCurrency").HasMaxLength(10).IsRequired();
        });

        builder.OwnsOne(e => e.ReservedBalance, rb =>
        {
            rb.Property(m => m.Amount).HasColumnName("ReservedBalance").HasColumnType("decimal(18,2)").IsRequired();
            rb.Property(m => m.Currency).HasColumnName("ReservedCurrency").HasMaxLength(10).IsRequired();
        });

        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.OwnerId).IsUnique().HasDatabaseName("IX_Wallets_UserId");

        builder.HasMany(e => e.ActiveReservations)
            .WithOne(r => r.Wallet)
            .HasForeignKey("WalletId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Domain.Wallet.Aggregates.Wallet.ActiveReservations))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}