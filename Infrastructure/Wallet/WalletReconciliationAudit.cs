namespace Infrastructure.Wallet;

public sealed class WalletReconciliationAudit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public decimal SnapshotBalance { get; init; }
    public decimal LedgerBalance { get; init; }
    public decimal Delta { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
    public string? Notes { get; init; }
}