namespace Infrastructure.BackgroundJobs;

public sealed class WalletReconciliationJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(45);
    private const int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:wallet-reconciliation", LockExpiry, ct);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    await RunReconciliationAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                using var errorScope = scopeFactory.CreateScope();
                await errorScope.ServiceProvider.GetRequiredService<IAuditService>()
                    .LogSystemEventAsync(
                        "WalletReconciliationError",
                        $"خطا در سرویس انطباق کیف پول: {ex.Message}",
                        ct);
            }

            await Task.Delay(CheckInterval, ct);
        }
    }

    private async Task RunReconciliationAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var offset = 0;

        while (true)
        {
            var walletBatch = await context.Wallets
                .AsNoTracking()
                .Where(w => w.IsActive)
                .OrderBy(w => w.Id)
                .Skip(offset)
                .Take(BatchSize)
                .Select(w => new { w.Id, BalanceAmount = w.Balance.Amount })
                .ToListAsync(ct);

            if (walletBatch.Count == 0)
                break;

            var batchIds = walletBatch.Select(w => w.Id).ToList();

            var ledgerSums = await context.WalletLedgerEntries
                .AsNoTracking()
                .Where(e => batchIds.Contains(e.WalletId))
                .GroupBy(e => e.WalletId)
                .Select(g => new { WalletId = g.Key, Total = g.Sum(e => e.Amount.Amount) })
                .ToListAsync(ct);

            var ledgerLookup = ledgerSums.ToDictionary(l => l.WalletId, l => l.Total);

            foreach (var wallet in walletBatch)
            {
                var ledgerSum = ledgerLookup.GetValueOrDefault(wallet.Id, 0m);
                var diff = Math.Abs(wallet.BalanceAmount - ledgerSum);

                if (diff > 0.01m)
                {
                    await auditService.LogSystemEventAsync(
                        "WalletReconciliationDiscrepancy",
                        $"کیف پول {wallet.Id.Value}: مغایرت {diff} ریال یافت شد. موجودی: {wallet.BalanceAmount}، جمع دفتر: {ledgerSum}",
                        ct);
                }
            }

            offset += BatchSize;
        }
    }
}