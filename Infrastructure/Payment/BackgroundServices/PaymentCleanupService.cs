namespace Infrastructure.Payment.BackgroundServices;

public class PaymentCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public PaymentCleanupService(IServiceProvider serviceProvider, ILogger<PaymentCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Cleanup Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up abandoned payments.");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Payment Cleanup Service stopped.");
    }

    private async Task ProcessCleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var cutoff = DateTime.UtcNow.AddMinutes(-20);

        var result = await mediator.Send(
            new ExpireStalePaymentsCommand(cutoff),
            stoppingToken);

        if (result.IsSucceed && result.Data > 0)
        {
            _logger.LogInformation("Payment cleanup: Expired {Count} stale transactions.", result.Data);
        }
    }
}