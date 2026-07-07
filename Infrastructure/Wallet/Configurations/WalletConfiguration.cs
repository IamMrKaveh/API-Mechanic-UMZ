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

        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.Property(e => e.FreezeReason)
            .HasMaxLength(500);

        builder.Property(e => e.FrozenAt);

        builder.Property(e => e.FrozenBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? Domain.User.ValueObjects.UserId.From(value.Value) : null)
            .HasColumnName("FrozenBy");

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => e.OwnerId).IsUnique().HasDatabaseName("IX_Wallets_UserId");
        builder.HasIndex(e => e.IsActive).HasDatabaseName("IX_Wallets_IsActive");

        builder.HasMany(e => e.ActiveReservations)
            .WithOne(r => r.Wallet)
            .HasForeignKey("WalletId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Domain.Wallet.Aggregates.Wallet.ActiveReservations))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(e => e.Owner.IsActive);
    }
}