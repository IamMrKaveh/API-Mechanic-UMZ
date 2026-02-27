using Infrastructure.Search.Options;

namespace Infrastructure.Search.BackgroundServices;

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
        var elasticOptions = _configuration.GetSection(ElasticsearchOptions.SectionName)
            .Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();

        // بررسی فعال بودن سرویس
        if (!elasticOptions.IsEnabled || !elasticOptions.EnableBackgroundSync)
        {
            _logger.LogInformation("Elasticsearch sync background service is disabled");
            return;
        }

        var intervalMinutes = elasticOptions.Sync.IntervalMinutes;
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