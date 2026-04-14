using Infrastructure.Persistence.Outbox;
using Quartz;

namespace Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class OutboxProcessingJob(
    IOutboxProcessor outboxProcessor,
    ILogger<OutboxProcessingJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Processing outbox messages...");
        await outboxProcessor.ProcessAsync(context.CancellationToken);
    }
}