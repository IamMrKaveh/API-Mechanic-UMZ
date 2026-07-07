using Domain.Wallet.Interfaces;

namespace Infrastructure.BackgroundJobs;

public sealed class WalletTopUpCleanupJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    ILogger<WalletTopUpCleanupJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(4);
    private static readonly TimeSpan PendingCutoff = TimeSpan.FromMinutes(30);
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:wallet-topup-cleanup",
                    LockExpiry,
                    ct);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    await RunCleanupAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WalletTopUpCleanupJob encountered an error.");
            }

            try
            {
                await Task.Delay(Interval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWalletTopUpRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var cutoff = DateTime.UtcNow - PendingCutoff;
        var staleTopUps = await repository.GetPendingOlderThanAsync(cutoff, BatchSize, ct);

        if (staleTopUps.Count == 0) return;

        foreach (var topUp in staleTopUps)
        {
            topUp.MarkFailed("زمان درخواست شارژ منقضی شد (Timeout).");
            repository.Update(topUp);
        }

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("WalletTopUpCleanupJob expired {Count} stale top-ups.", staleTopUps.Count);
    }
}