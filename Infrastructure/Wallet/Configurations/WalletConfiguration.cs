namespace Infrastructure.Wallet.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Domain.Wallet.Wallet>
{
    public void Configure(EntityTypeBuilder<Domain.Wallet.Wallet> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.RowVersion).IsRowVersion();

        builder.HasIndex(w => w.UserId).IsUnique();

        builder.Property(w => w.CurrentBalance)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(w => w.ReservedBalance)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.UpdatedAt);

        builder.Ignore(w => w.AvailableBalance);
        builder.Ignore(w => w.IsActive);

        builder.HasMany(w => w.LedgerEntries)
            .WithOne()
            .HasForeignKey(e => e.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Reservations)
            .WithOne()
            .HasForeignKey(r => r.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Wallet_CurrentBalance_NonNegative", "\"CurrentBalance\" >= 0");
            t.HasCheckConstraint("CK_Wallet_ReservedBalance_NonNegative", "\"ReservedBalance\" >= 0");
        });
    }
}