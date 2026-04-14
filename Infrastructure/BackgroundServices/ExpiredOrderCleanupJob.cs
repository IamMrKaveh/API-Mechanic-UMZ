using Domain.Common.Interfaces;
using Domain.Order.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public sealed class ExpiredOrderCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiredOrderCleanupJob> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var expiredOrders = await orderRepo.GetExpiredUnpaidOrdersAsync(stoppingToken);

                foreach (var order in expiredOrders)
                {
                    order.Expire();
                    orderRepo.Update(order);
                }

                if (expiredOrders.Any())
                {
                    await unitOfWork.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Expired {Count} unpaid orders.", expiredOrders.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ExpiredOrderCleanupJob.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}