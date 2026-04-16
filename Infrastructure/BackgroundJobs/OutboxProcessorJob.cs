using Infrastructure.Persistence.Outbox;

namespace Infrastructure.BackgroundJobs;

public sealed class OutboxProcessorJob(
    IServiceScopeFactory scopeFactory,
    IAuditService auditService) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await auditService.LogDebugAsync("OutboxProcessorBackgroundService started.", ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                await processor.ProcessAsync(50, ct);
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(ex.Message, ct);
            }

            await Task.Delay(_interval, ct);
        }
    }
}