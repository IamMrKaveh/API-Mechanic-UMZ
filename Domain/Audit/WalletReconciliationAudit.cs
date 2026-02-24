namespace Domain.Audit;

/// <summary>Audit record written when a balance discrepancy is detected.</summary>
public class WalletReconciliationAudit
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public int UserId { get; set; }
    public decimal SnapshotBalance { get; set; }
    public decimal LedgerBalance { get; set; }
    public decimal Delta { get; set; }
    public DateTime DetectedAt { get; set; }
}