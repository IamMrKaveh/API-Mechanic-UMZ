namespace Infrastructure.Payment.BackgroundServices;

public class PaymentCleanupService(
    IServiceProvider serviceProvider,
    IAuditService auditService) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await auditService.LogInformationAsync("Payment Cleanup Service started.", ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessCleanupAsync(ct);
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(ex.Message, ct);
            }

            await Task.Delay(_interval, ct);
        }
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
            await auditService.LogInformationAsync("Payment cleanup: Expired {Count} stale transactions.", ct);
        }
    }
}