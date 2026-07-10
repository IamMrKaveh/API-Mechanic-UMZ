using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.FraudDetection;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.BackgroundJobs;

public sealed class FraudDetectionJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    IDateTimeProvider dateTimeProvider) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan EvaluationWindow = TimeSpan.FromHours(1);
    private static readonly TimeSpan AlertCooldown = TimeSpan.FromHours(6);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using (var startScope = scopeFactory.CreateScope())
        {
            await startScope.ServiceProvider.GetRequiredService<IAuditService>()
                .LogSystemEventAsync("FraudDetection", "Fraud Detection Service started.", ct);
        }

        await Task.Delay(InitialDelay, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:fraud-detection", LockExpiry, ct);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    await RunDetectionAsync(ct);
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
                    .LogErrorAsync($"Error during fraud detection: {ex.Message}", ct);
            }

            await Task.Delay(CheckInterval, ct);
        }

        using var stopScope = scopeFactory.CreateScope();
        await stopScope.ServiceProvider.GetRequiredService<IAuditService>()
            .LogSystemEventAsync("FraudDetection", "Fraud Detection Service stopped.", ct);
    }

    private async Task RunDetectionAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
        var alertRepository = scope.ServiceProvider.GetRequiredService<IWalletFraudAlertRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var rules = scope.ServiceProvider.GetServices<IFraudDetectionRule>().ToList();

        if (rules.Count == 0)
        {
            await auditService.LogWarningAsync("[FraudDetection] هیچ Rule فعالی یافت نشد.", ct);
            return;
        }

        var cutoff = dateTimeProvider.UtcNow.Subtract(EvaluationWindow);

        var activeWalletIds = await dbContext.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OccurredAt >= cutoff)
            .Select(e => e.WalletId)
            .Distinct()
            .Take(BatchSize)
            .ToListAsync(ct);

        var totalTriggered = 0;

        foreach (var walletId in activeWalletIds)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var evaluated = await EvaluateWalletAsync(
                    dbContext,
                    alertRepository,
                    rules,
                    walletId,
                    dateTimeProvider,
                    ct);

                totalTriggered += evaluated;
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(
                    $"[FraudDetection] خطا در ارزیابی کیف پول {walletId.Value}: {ex.Message}",
                    ct);
            }
        }

        if (totalTriggered > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
            await auditService.LogSystemEventAsync(
                "FraudDetection",
                $"[FraudDetection] {totalTriggered} هشدار جدید ثبت شد.",
                ct);
        }
    }

    private static async Task<int> EvaluateWalletAsync(
        DBContext dbContext,
        IWalletFraudAlertRepository alertRepository,
        IReadOnlyList<IFraudDetectionRule> rules,
        WalletId walletId,
        IDateTimeProvider dateTimeProvider,
        CancellationToken ct)
    {
        var evaluatedAt = dateTimeProvider.UtcNow;
        var windowStart = evaluatedAt.Subtract(EvaluationWindow);

        var wallet = await dbContext.Wallets
            .AsNoTracking()
            .Where(w => w.Id == walletId)
            .Select(w => new { w.OwnerId })
            .FirstOrDefaultAsync(ct);

        if (wallet is null) return 0;

        var userId = wallet.OwnerId;

        var recentEntries = await dbContext.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.WalletId == walletId && e.OccurredAt >= windowStart)
            .OrderByDescending(e => e.OccurredAt)
            .Take(200)
            .ToListAsync(ct);

        var userAverage = await dbContext.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OwnerId == userId)
            .Select(e => e.Amount.Amount)
            .DefaultIfEmpty(0m)
            .AverageAsync(ct);

        var recentTopUpCount = await dbContext.WalletTopUps
            .AsNoTracking()
            .CountAsync(t =>
                t.UserId == userId
                && t.CreatedAt >= windowStart
                && t.Status == WalletTopUpStatus.Succeeded,
                ct);

        var recentFailedTopUpCount = await dbContext.WalletTopUps
            .AsNoTracking()
            .CountAsync(t =>
                t.UserId == userId
                && t.CreatedAt >= windowStart
                && (t.Status == WalletTopUpStatus.Failed || t.Status == WalletTopUpStatus.Cancelled),
                ct);

        var recentWithdrawalCount = await dbContext.WalletWithdrawalRequests
            .AsNoTracking()
            .CountAsync(w =>
                w.UserId == userId
                && w.CreatedAt >= windowStart,
                ct);

        var context = new FraudEvaluationContext
        {
            WalletId = walletId,
            UserId = userId,
            RecentLedgerEntries = recentEntries,
            UserAverageAmount = userAverage,
            RecentTopUpCount = recentTopUpCount,
            RecentFailedTopUpCount = recentFailedTopUpCount,
            RecentWithdrawalCount = recentWithdrawalCount,
            EvaluatedAt = evaluatedAt
        };

        var triggered = 0;

        foreach (var rule in rules)
        {
            var result = await rule.EvaluateAsync(context, ct);
            if (!result.IsTriggered) continue;

            var hasRecent = await alertRepository.HasRecentAlertAsync(walletId, rule.RuleName, AlertCooldown, ct);
            if (hasRecent) continue;

            var alert = WalletFraudAlert.Raise(
                walletId,
                userId,
                result.RuleName,
                result.Severity,
                result.Description,
                result.Metadata);

            await alertRepository.AddAsync(alert, ct);
            triggered++;
        }

        return triggered;
    }
}