namespace Infrastructure.Payment.BackgroundServices;

public class PaymentCleanupService(IServiceProvider serviceProvider, ILogger<PaymentCleanupService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<PaymentCleanupService> _logger = logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Payment Cleanup Service started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessCleanupAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up abandoned payments.");
            }

            await Task.Delay(_interval, ct);
        }

        _logger.LogInformation("Payment Cleanup Service stopped.");
    }

    private async Task ProcessCleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var cutoff = DateTime.UtcNow.AddMinutes(-20);

        var result = await mediator.Send(
            new ExpireStalePaymentsCommand(cutoff),
            ct);

        if (result.IsSuccess && result.Value > 0)
        {
            _logger.LogInformation("Payment cleanup: Expired {Count} stale transactions.", result.Value);
        }
    }
}