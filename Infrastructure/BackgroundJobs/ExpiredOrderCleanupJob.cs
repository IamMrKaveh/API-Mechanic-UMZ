using Domain.Order.Interfaces;

namespace Infrastructure.BackgroundJobs;

public sealed class ExpiredOrderCleanupJob(
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var lockScope = scopeFactory.CreateScope();
                var distributedLock = lockScope.ServiceProvider.GetRequiredService<IDistributedLock>();
                
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:expired-order-cleanup", LockExpiry, stoppingToken);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    using var scope = scopeFactory.CreateScope();
                    var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

                    // Drain loop: each pass handles one bounded batch; run repeatedly
                    // while the repository keeps returning full batches so a backlog
                    // can actually shrink instead of one batch per 30 minutes.
                    const int batchSize = 500;
                    var totalExpired = 0;

                    while (totalExpired < batchSize * 10)
                    {
                        var expiredOrders = await orderRepo.FindPendingExpiredAsync(stoppingToken);
                        if (expiredOrders.Count == 0)
                            break;

                        foreach (var order in expiredOrders)
                        {
                            order.Expire(order.Status);
                            orderRepo.Update(order);
                        }

                        await unitOfWork.SaveChangesAsync(stoppingToken);
                        totalExpired += expiredOrders.Count;

                        if (expiredOrders.Count < batchSize)
                            break;
                    }

                    if (totalExpired > 0)
                    {
                        await auditService.LogSystemEventAsync(
                            "ExpiredOrderCleanup",
                            $"{totalExpired} سفارش منقضی‌شده پردازش شد.",
                            stoppingToken);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                using var scope = scopeFactory.CreateScope();
                var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
                await auditService.LogSystemEventAsync(
                    "ExpiredOrderCleanupError",
                    ex.Message,
                    stoppingToken);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}