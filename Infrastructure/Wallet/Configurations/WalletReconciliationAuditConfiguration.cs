namespace Infrastructure.Wallet.Configurations;

public sealed class WalletReconciliationAuditConfiguration
    : IEntityTypeConfiguration<WalletReconciliationAudit>
{
    public void Configure(EntityTypeBuilder<WalletReconciliationAudit> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.WalletId).IsRequired();
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.SnapshotBalance).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.LedgerBalance).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.Delta).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.DetectedAt).IsRequired();

        builder.HasIndex(e => e.WalletId);
        builder.HasIndex(e => e.DetectedAt);
    }
}