using Domain.Common.Interfaces;
using Domain.Security.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public sealed class ExpiredSessionCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiredSessionCleanupJob> logger) : BackgroundService
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

                var cutoff = DateTime.UtcNow.AddDays(-30);
                await sessionRepo.DeleteExpiredAsync(cutoff, stoppingToken);
                await unitOfWork.SaveChangesAsync(stoppingToken);

                logger.LogInformation("Expired sessions cleanup completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ExpiredSessionCleanupJob.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}