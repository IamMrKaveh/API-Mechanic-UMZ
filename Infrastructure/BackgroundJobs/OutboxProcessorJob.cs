using Application.Audit.Contracts;
using Infrastructure.Persistence.Outbox;

namespace Infrastructure.BackgroundJobs;

public sealed class OutboxProcessorJob(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using (var startupScope = scopeFactory.CreateScope())
        {
            var audit = startupScope.ServiceProvider.GetRequiredService<IAuditService>();
            await audit.LogDebugAsync("OutboxProcessorBackgroundService started.", ct);
        }

        while (!ct.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

            try
            {
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