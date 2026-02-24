namespace Infrastructure.Wallet.BackgroundServices;

/// <summary>
/// Daily reconciliation job that compares the wallet snapshot balances against
/// the ledger-derived totals and flags any discrepancies.
/// Mandatory for financial-grade systems.
/// </summary>
public class WalletReconciliationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WalletReconciliationService> _logger;

    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public WalletReconciliationService(
        IServiceScopeFactory scopeFactory,
        ILogger<WalletReconciliationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken st)
    {
        _logger.LogInformation("WalletReconciliationService started.");

        while (!st.IsCancellationRequested)
        {
            try
            {
                await RunReconciliationAsync(st);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WalletReconciliationService error.");
            }

            await Task.Delay(_interval, st);
        }
    }

    private async Task RunReconciliationAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var discrepancies = await dbContext.Wallets
            .Select(w => new
            {
                w.Id,
                w.UserId,
                SnapshotBalance = w.CurrentBalance,
                LedgerBalance = dbContext.WalletLedgerEntries
                    .Where(e => e.WalletId == w.Id)
                    .Sum(e => (decimal?)e.AmountDelta) ?? 0m
            })
            .Where(x => x.SnapshotBalance != x.LedgerBalance)
            .ToListAsync(ct);

        if (discrepancies.Count == 0)
        {
            _logger.LogInformation("WalletReconciliation: all {Count} wallets balanced.",
                await dbContext.Wallets.CountAsync(ct));
            return;
        }

        foreach (var d in discrepancies)
        {
            _logger.LogCritical(
                "WALLET DISCREPANCY: WalletId={WalletId} UserId={UserId} " +
                "Snapshot={Snapshot} Ledger={Ledger} Delta={Delta}",
                d.Id, d.UserId, d.SnapshotBalance, d.LedgerBalance,
                d.SnapshotBalance - d.LedgerBalance);
        }

        var auditEntries = discrepancies.Select(d => new WalletReconciliationAudit
        {
            WalletId = d.Id,
            UserId = d.UserId,
            SnapshotBalance = d.SnapshotBalance,
            LedgerBalance = d.LedgerBalance,
            Delta = d.SnapshotBalance - d.LedgerBalance,
            DetectedAt = DateTime.UtcNow
        });

        await dbContext.WalletReconciliationAudits.AddRangeAsync(auditEntries, ct);
        await dbContext.SaveChangesAsync(ct);
    }
}