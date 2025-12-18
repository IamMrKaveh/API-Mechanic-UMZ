namespace Infrastructure.BackgroundJobs;

public class ElasticsearchSyncBackgroundService : BackgroundService
{
    private readonly ILogger<ElasticsearchSyncBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(6);
    private readonly TimeSpan _initialDelay = TimeSpan.FromMinutes(5);

    public ElasticsearchSyncBackgroundService(
        ILogger<ElasticsearchSyncBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Elasticsearch Sync Background Service is starting");

        await Task.Delay(_initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformSyncAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Elasticsearch sync");
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }

        _logger.LogInformation("Elasticsearch Sync Background Service is stopping");
    }

    private async Task PerformSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var indexManager = scope.ServiceProvider.GetRequiredService<IElasticIndexManager>();
        var databaseSyncService = scope.ServiceProvider.GetRequiredService<ElasticsearchDatabaseSyncService>();

        _logger.LogInformation("Starting periodic Elasticsearch sync from Supabase database");

        var productsExist = await indexManager.IndexExistsAsync("products_v1", cancellationToken);
        var categoriesExist = await indexManager.IndexExistsAsync("categories_v1", cancellationToken);
        var categoryGroupsExist = await indexManager.IndexExistsAsync("categorygroups_v1", cancellationToken);

        if (!productsExist || !categoriesExist || !categoryGroupsExist)
        {
            _logger.LogWarning("One or more indices do not exist. Creating missing indices...");
            await indexManager.CreateAllIndicesAsync(cancellationToken);
        }

        await databaseSyncService.FullSyncAsync(cancellationToken);

        _logger.LogInformation("Periodic Elasticsearch sync from Supabase completed");
    }
}