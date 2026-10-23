using Domain.Security.Interfaces;

namespace Infrastructure.BackgroundJobs;

public sealed class ExpiredSessionCleanupJob(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider dateTimeProvider) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var lockScope = scopeFactory.CreateScope();
                var distributedLock = lockScope.ServiceProvider.GetRequiredService<IDistributedLock>();
                
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:expired-session-cleanup", LockExpiry, stoppingToken);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    using var scope = scopeFactory.CreateScope();
                    var sessionRepo = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

                    var cutoff = dateTimeProvider.UtcNow;

                    // Drain loop: each pass handles one bounded batch (the repository
                    // caps at 1000 rows) so a growing backlog of expired sessions
                    // drains instead of one batch per hour.
                    const int batchSize = 1000;
                    var totalExpired = 0;

                    while (totalExpired < batchSize * 10)
                    {
                        var expiredSessions = await sessionRepo.GetExpiredActiveSessionsAsync(cutoff, stoppingToken);
                        if (expiredSessions.Count == 0)
                            break;

                        foreach (var session in expiredSessions)
                            session.MarkExpired(cutoff);

                        await unitOfWork.SaveChangesAsync(stoppingToken);
                        totalExpired += expiredSessions.Count;

                        if (expiredSessions.Count < batchSize)
                            break;
                    }

                    if (totalExpired > 0)
                    {
                        await auditService.LogSystemEventAsync(
                            "ExpiredSessionCleanup",
                            $"{totalExpired} نشست منقضی‌شده پاکسازی شد.",
                            stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                using var scope = scopeFactory.CreateScope();
                var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
                await auditService.LogSystemEventAsync(
                    "ExpiredSessionCleanupError",
                    ex.Message,
                    stoppingToken);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}