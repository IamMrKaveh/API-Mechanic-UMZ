namespace Infrastructure.Search.BackgroundServices;

/// <summary>
/// همگام‌سازی دوره‌ای دیتابیس با Elasticsearch
/// </summary>
public class ElasticsearchSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ElasticsearchSyncBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public ElasticsearchSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ElasticsearchSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue("Elasticsearch:Sync:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("Elasticsearch sync background service is disabled");
            return;
        }

        var intervalMinutes = _configuration.GetValue("Elasticsearch:Sync:IntervalMinutes", 5);

        _logger.LogInformation(
            "Elasticsearch sync background service started. Interval: {Interval} minutes",
            intervalMinutes);

        
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ElasticsearchDatabaseSyncService>();

                _logger.LogDebug("Starting periodic Elasticsearch sync");
                await syncService.FullSyncAsync(stoppingToken);
                _logger.LogDebug("Periodic Elasticsearch sync completed");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Elasticsearch sync background service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic Elasticsearch sync");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}