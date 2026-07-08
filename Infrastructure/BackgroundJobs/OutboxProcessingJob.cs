using Infrastructure.BackgroundJobs.Common;
using Infrastructure.Persistence.Outbox;

namespace Infrastructure.BackgroundJobs;

public sealed class OutboxProcessingJob(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessingJob> logger) : DistributedLockedBackgroundService(scopeFactory, logger)
{
    protected override string LockKey => "jobs:outbox-processing";
    protected override TimeSpan Interval => TimeSpan.FromSeconds(15);
    protected override TimeSpan LockExpiry => TimeSpan.FromMinutes(2);

    protected override async Task ExecuteInsideLockAsync(IServiceProvider services, CancellationToken ct)
    {
        var processor = services.GetRequiredService<IOutboxProcessor>();
        await processor.ProcessAsync(ct: ct);
    }
}