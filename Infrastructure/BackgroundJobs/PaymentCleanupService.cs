namespace Infrastructure.BackgroundJobs;

public class PaymentCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentCleanupService> _logger;
    private int _errorCount = 0;
    private const int MaxErrorThreshold = 5;

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

                    var cancelledStatus = await dbContext.OrderStatuses
                        .FirstOrDefaultAsync(s => s.Name == "Cancelled" || s.Name == "Expired" || s.Name == "لغو شده", stoppingToken);

                    foreach (var tx in stuckTransactions)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        tx.Status = "Expired";

                        if (tx.Order != null && !tx.Order.IsPaid && tx.Order.OrderStatusId != cancelledStatus?.Id)
                        {
                            if (cancelledStatus != null)
                            {
                                tx.Order.OrderStatusId = cancelledStatus.Id;
                            }
                            else
                            {
                                tx.Order.IsDeleted = true;
                                tx.Order.DeletedAt = DateTime.UtcNow;
                                tx.Order.DeletedBy = 0;
                            }

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

                _errorCount = 0;
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Payment Cleanup Service stopping.");
                break;
            }
            catch (Exception ex)
            {
                _errorCount++;
                _logger.LogError(ex, "Error occurred while cleaning up abandoned payments. Error count: {Count}", _errorCount);

                if (_errorCount >= MaxErrorThreshold)
                {
                    _logger.LogCritical("PaymentCleanupService has failed {Count} times consecutively. Immediate attention required.", _errorCount);
                }

                var delaySeconds = Math.Min(300, Math.Pow(2, _errorCount));
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}