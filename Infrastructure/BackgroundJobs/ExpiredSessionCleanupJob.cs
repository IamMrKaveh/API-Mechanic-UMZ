using Domain.Security.Interfaces;

namespace Infrastructure.BackgroundJobs;

public sealed class ExpiredSessionCleanupJob(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sessionRepo = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

                var cutoff = DateTime.UtcNow;
                var expiredSessions = await sessionRepo.GetExpiredActiveSessionsAsync(cutoff, stoppingToken);

                foreach (var session in expiredSessions)
                    session.MarkExpired();

                if (expiredSessions.Any())
                {
                    await unitOfWork.SaveChangesAsync(stoppingToken);
                    await auditService.LogSystemEventAsync(
                        "ExpiredSessionCleanup",
                        $"{expiredSessions.Count} نشست منقضی‌شده پاکسازی شد.",
                        stoppingToken);
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