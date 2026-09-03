using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Configurations;

public sealed class WalletWithdrawalRequestConfiguration : IEntityTypeConfiguration<WalletWithdrawalRequest>
{
    private static readonly ValueConverter<WalletWithdrawalStatus, string> StatusConverter =
        new(v => v.ToString(),
            v => (WalletWithdrawalStatus)Enum.Parse(typeof(WalletWithdrawalStatus), v));

    private static readonly ValueConverter<IbanNumber, string> IbanConverter =
        new(v => v.Value, v => IbanNumber.Create(v));

    public void Configure(EntityTypeBuilder<WalletWithdrawalRequest> builder)
    {
        builder.ToTable("WalletWithdrawalRequests");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WalletWithdrawalRequestId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .HasColumnName("UserId").IsRequired();

        builder.OwnsOne(e => e.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            m.Property(x => x.Currency).HasColumnName("AmountCurrency").HasMaxLength(10).IsRequired();
        });

        var ibanProp = builder.Property(e => e.Iban)
            .HasConversion(IbanConverter)
            .HasColumnName("Iban")
            .HasColumnType("varchar(32)")
            .HasMaxLength(32)
            .IsRequired();
        ibanProp.Metadata.SetProviderClrType(typeof(string));

        builder.Property(e => e.AccountHolder).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.Property(e => e.ReservationId)
            .HasConversion(id => id.Value, value => WalletReservationId.From(value))
            .HasColumnName("ReservationId").IsRequired();

        var statusProp = builder.Property(e => e.Status)
            .HasConversion(StatusConverter)
            .HasColumnType("varchar(32)")
            .HasMaxLength(32)
            .IsRequired();
        statusProp.Metadata.SetProviderClrType(typeof(string));

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.ApprovedAt);
        builder.Property(e => e.RejectedAt);
        builder.Property(e => e.PaidAt);
        builder.Property(e => e.CancelledAt);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);
        builder.Property(e => e.BankReferenceNumber).HasMaxLength(64);

        builder.Property(e => e.ProcessedBy)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value.HasValue ? UserId.From(value.Value) : null);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin").HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        builder.HasIndex(e => e.UserId).HasDatabaseName("IX_WalletWithdrawalRequests_UserId");
        builder.HasIndex(e => e.Status).HasDatabaseName("IX_WalletWithdrawalRequests_Status");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_WalletWithdrawalRequests_CreatedAt");
        builder.HasIndex(e => e.ReservationId).IsUnique().HasDatabaseName("IX_WalletWithdrawalRequests_ReservationId");

        builder.Ignore(e => e.DomainEvents);
    }
}
