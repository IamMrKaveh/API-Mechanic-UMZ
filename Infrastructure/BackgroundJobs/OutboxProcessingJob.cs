using Infrastructure.Persistence.Outbox;
using Application.Audit.Contracts;
using Quartz;

namespace Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class OutboxProcessingJob(
    IOutboxProcessor outboxProcessor,
    IAuditService auditService) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            await outboxProcessor.ProcessAsync(ct: context.CancellationToken);
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "OutboxProcessingError",
                ex.Message,
                context.CancellationToken);
        }
    }
}