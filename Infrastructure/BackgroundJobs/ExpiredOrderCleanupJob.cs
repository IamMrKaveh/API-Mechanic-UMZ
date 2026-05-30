using Domain.Order.Interfaces;

namespace Infrastructure.BackgroundJobs;

public sealed class ExpiredOrderCleanupJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:expired-order-cleanup", LockExpiry, stoppingToken);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    using var scope = scopeFactory.CreateScope();
                    var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

                    var expiredOrders = await orderRepo.FindPendingExpiredAsync(stoppingToken);

                    foreach (var order in expiredOrders)
                    {
                        order.Expire(order.Status);
                        orderRepo.Update(order);
                    }

                    if (expiredOrders.Any())
                    {
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                        await auditService.LogSystemEventAsync(
                            "ExpiredOrderCleanup",
                            $"{expiredOrders.Count} سفارش منقضی‌شده پردازش شد.",
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