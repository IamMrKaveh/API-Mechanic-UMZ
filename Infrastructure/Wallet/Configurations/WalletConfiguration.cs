using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Domain.Wallet.Aggregates.Wallet>
{
    public void Configure(EntityTypeBuilder<Domain.Wallet.Aggregates.Wallet> builder)
    {
        builder.ToTable("Wallets");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WalletId.From(value));

        builder.Property(e => e.OwnerId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("UserId")
            .IsRequired();

        builder.OwnsOne(e => e.Balance, b =>
        {
            b.Property(m => m.Amount)
                .HasColumnName("CurrentBalance")
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m)
                .IsRequired();
            b.Property(m => m.Currency)
                .HasColumnName("BalanceCurrency")
                .HasMaxLength(10)
                .HasDefaultValue("IRT")
                .IsRequired();
        });

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.Property(e => e.FreezeReason).HasMaxLength(500);
        builder.Property(e => e.FrozenAt);
        builder.Property(e => e.FrozenBy)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserId.From(value.Value) : null)
            .HasColumnName("FrozenBy");

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => e.OwnerId).IsUnique().HasDatabaseName("IX_Wallets_UserId");
        builder.HasIndex(e => e.IsActive).HasDatabaseName("IX_Wallets_IsActive");

        builder.HasMany(w => w.DebitRequests)
            .WithOne()
            .HasForeignKey("WalletId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(e => e.Owner.IsActive);
    }
}
