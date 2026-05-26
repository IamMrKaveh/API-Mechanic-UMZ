namespace Infrastructure.BackgroundJobs;

public sealed class WalletReconciliationJob(
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunReconciliationAsync(ct);
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

        var discrepancies = await context.Wallets
            .AsNoTracking()
            .Where(w => w.IsActive)
            .Select(w => new
            {
                w.Id,
                BalanceAmount = w.Balance.Amount,
                LedgerSum = context.WalletLedgerEntries
                    .Where(e => e.WalletId == w.Id)
                    .Sum(e => (decimal?)e.Amount.Amount) ?? 0m
            })
            .Where(x => Math.Abs(x.BalanceAmount - x.LedgerSum) > 0.01m)
            .ToListAsync(ct);

        foreach (var item in discrepancies)
        {
            var diff = Math.Abs(item.BalanceAmount - item.LedgerSum);
            await auditService.LogSystemEventAsync(
                "WalletReconciliationDiscrepancy",
                $"کیف پول {item.Id.Value}: مغایرت {diff} ریال یافت شد. موجودی: {item.BalanceAmount}، جمع دفتر: {item.LedgerSum}",
                ct);
        }
    }
}