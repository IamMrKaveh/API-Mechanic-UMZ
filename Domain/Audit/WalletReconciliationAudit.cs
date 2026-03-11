namespace Domain.Audit;

public class WalletReconciliationAudit : Entity
{
    public int WalletId { get; private set; }
    public int UserId { get; private set; }
    public decimal SnapshotBalance { get; private set; }
    public decimal LedgerBalance { get; private set; }
    public decimal Delta { get; private set; }
    public DateTime DetectedAt { get; private set; }

    private WalletReconciliationAudit()
    { }

    public static WalletReconciliationAudit Create(
        int walletId,
        int userId,
        decimal snapshotBalance,
        decimal ledgerBalance)
    {
        if (walletId <= 0) throw new ArgumentOutOfRangeException(nameof(walletId));
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));

        return new WalletReconciliationAudit
        {
            WalletId = walletId,
            UserId = userId,
            SnapshotBalance = snapshotBalance,
            LedgerBalance = ledgerBalance,
            Delta = snapshotBalance - ledgerBalance,
            DetectedAt = DateTime.UtcNow
        };
    }
}