namespace Infrastructure.Order.BackgroundServices;

/// <summary>
/// سرویس پس‌زمینه برای انقضای خودکار سفارش‌های پرداخت‌نشده.
/// هر دقیقه اجرا می‌شود و سفارش‌های قدیمی را Expire می‌کند.
/// </summary>
public sealed class OrderExpiryBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderExpiryBackgroundService> _logger;

    public OrderExpiryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OrderExpiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order Expiry Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunExpiryCheckAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in Order Expiry Service.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("Order Expiry Service stopped.");
    }

    private async Task RunExpiryCheckAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new ExpireOrdersCommand(), ct);

        if (result.ExpiredCount > 0)
        {
            _logger.LogInformation(
                "Order Expiry Service: {Count} orders expired. IDs: [{Ids}]",
                result.ExpiredCount,
                string.Join(", ", result.ExpiredOrderIds));
        }
    }
}