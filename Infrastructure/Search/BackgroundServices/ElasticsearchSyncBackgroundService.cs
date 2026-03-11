namespace Infrastructure.Search.BackgroundServices;

public class ElasticsearchSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ElasticsearchSyncBackgroundService> logger,
    IConfiguration configuration) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<ElasticsearchSyncBackgroundService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    protected override async Task ExecuteAsync(CancellationToken ct)
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

        await Task.Delay(TimeSpan.FromSeconds(30), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ElasticsearchDatabaseSyncService>();
                _logger.LogDebug("Starting periodic Elasticsearch sync");
                await syncService.FullSyncAsync(ct);
                _logger.LogDebug("Periodic Elasticsearch sync completed");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("Elasticsearch sync background service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic Elasticsearch sync");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), ct);
        }
    }
}