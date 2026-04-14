using Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public sealed class OutboxProcessorBackgroundJob(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorBackgroundJob> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxProcessorBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                await processor.ProcessAsync(50, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OutboxProcessorBackgroundService.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}