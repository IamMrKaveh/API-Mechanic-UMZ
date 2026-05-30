using Infrastructure.Persistence.Outbox;

namespace Infrastructure.BackgroundJobs;

public sealed class OutboxProcessingJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock) : BackgroundService
{
    private static readonly TimeSpan ProcessingInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:outbox-processing", LockExpiry, ct);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    using var scope = scopeFactory.CreateScope();
                    var outboxProcessor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                    await outboxProcessor.ProcessAsync(ct: ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                using var errorScope = scopeFactory.CreateScope();
                await errorScope.ServiceProvider.GetRequiredService<IAuditService>()
                    .LogSystemEventAsync("OutboxProcessingError", ex.Message, ct);
            }

            await Task.Delay(ProcessingInterval, ct);
        }
    }
}