namespace Infrastructure.BackgroundJobs;

public class PaymentCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentCleanupService> _logger;

    public PaymentCleanupService(IServiceProvider serviceProvider, ILogger<PaymentCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<LedkaContext>();
                    var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
                    var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

                    var cutoff = DateTime.UtcNow.AddMinutes(-20);

                    var stuckTransactions = await dbContext.PaymentTransactions
                        .Where(pt => pt.Status == "Pending" && pt.CreatedAt < cutoff)
                        .Include(pt => pt.Order)
                        .ThenInclude(o => o.OrderItems)
                        .ToListAsync(stoppingToken);

                    foreach (var tx in stuckTransactions)
                    {
                        tx.Status = "Expired";
                        if (tx.Order != null && tx.Order.OrderStatusId == 1)
                        {
                            tx.Order.IsDeleted = true;
                            tx.Order.DeletedAt = DateTime.UtcNow;
                            tx.Order.DeletedBy = 0;

                            foreach (var item in tx.Order.OrderItems)
                            {
                                await inventoryService.LogTransactionAsync(
                                    item.VariantId,
                                    "ReservationRollback",
                                    item.Quantity,
                                    item.Id,
                                    tx.Order.UserId,
                                    $"System rollback for expired order {tx.Order.Id}",
                                    $"ORDER-ROLLBACK-{tx.Order.Id}",
                                    null,
                                    false
                                );
                            }
                        }

                        await auditService.LogSystemEventAsync("PaymentExpired", $"Tx {tx.Authority} expired.");
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up abandoned payments.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}