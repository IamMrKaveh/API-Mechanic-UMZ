namespace Infrastructure.BackgroundJobs;

public sealed class WalletReconciliationJob(
    IServiceProvider serviceProvider,
    IAuditService auditService) : BackgroundService
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
                await auditService.LogSystemEventAsync(
                    "WalletReconciliationError",
                    $"خطا در سرویس انطباق کیف پول: {ex.Message}",
                    ct);
            }

            await Task.Delay(CheckInterval, ct);
        }
    }

    private async Task RunReconciliationAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();

        var wallets = await context.Wallets
            .AsNoTracking()
            .Where(w => w.IsActive)
            .ToListAsync(ct);

        foreach (var wallet in wallets)
        {
            var ledgerSum = await context.WalletLedgerEntries
                .AsNoTracking()
                .Where(e => e.WalletId == wallet.Id)
                .SumAsync(e => e.Amount.Amount, ct);

            var diff = Math.Abs(wallet.Balance.Amount - ledgerSum);

            if (diff > 0.01m)
            {
                await auditService.LogSystemEventAsync(
                    "WalletReconciliationDiscrepancy",
                    $"کیف پول {wallet.Id.Value}: مغایرت {diff} ریال یافت شد. موجودی: {wallet.Balance.Amount}، جمع دفتر: {ledgerSum}",
                    ct);
            }
        }
    }
}