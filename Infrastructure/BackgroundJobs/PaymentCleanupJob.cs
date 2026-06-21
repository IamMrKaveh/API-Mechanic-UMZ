using Application.Payment.Features.Commands.ExpireStalePayments;

namespace Infrastructure.BackgroundJobs;

public sealed class PaymentCleanupJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock) : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using (var startScope = scopeFactory.CreateScope())
        {
            await startScope.ServiceProvider.GetRequiredService<IAuditService>()
                .LogInformationAsync("Payment Cleanup Service started.", ct);
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:payment-cleanup", LockExpiry, ct);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    await ProcessCleanupAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                using var errorScope = scopeFactory.CreateScope();
                await errorScope.ServiceProvider.GetRequiredService<IAuditService>()
                    .LogErrorAsync(ex.Message, ct);
            }

            await Task.Delay(_interval, ct);
        }
    }

    private async Task ProcessCleanupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var cutoff = DateTime.UtcNow.AddMinutes(-20);

        var result = await mediator.Send(new ExpireStalePaymentsCommand(cutoff), ct);

        if (result.IsSuccess && result.Value > 0)
        {
            await auditService.LogInformationAsync("Payment cleanup: Expired {Count} stale transactions.", ct);
        }
    }
}