using Infrastructure.Search.Interfaces;

namespace Infrastructure.Search;

/// <summary>
/// Background service for periodic Elasticsearch index refresh and health monitoring
/// </summary>
public class ElasticsearchSyncBackgroundService : BackgroundService
{
    private readonly ILogger<ElasticsearchSyncBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(30);

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

        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

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

        _logger.LogInformation("Starting Elasticsearch sync check");

        var productsExist = await indexManager.IndexExistsAsync("products_v1", cancellationToken);
        var categoriesExist = await indexManager.IndexExistsAsync("categories_v1", cancellationToken);
        var categoryGroupsExist = await indexManager.IndexExistsAsync("categorygroups_v1", cancellationToken);

        if (!productsExist || !categoriesExist || !categoryGroupsExist)
        {
            _logger.LogWarning("One or more indices do not exist. Creating missing indices...");
            await indexManager.CreateAllIndicesAsync(cancellationToken);
        }

        _logger.LogInformation("Elasticsearch sync check completed");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Elasticsearch Sync Background Service is stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Startup task to ensure Elasticsearch indices are created on application start
/// </summary>
public class ElasticsearchStartupTask : IHostedService
{
    private readonly ILogger<ElasticsearchStartupTask> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ElasticsearchStartupTask(
        ILogger<ElasticsearchStartupTask> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running Elasticsearch startup tasks");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var indexManager = scope.ServiceProvider.GetRequiredService<IElasticIndexManager>();

            var success = await indexManager.CreateAllIndicesAsync(cancellationToken);

            if (success)
            {
                _logger.LogInformation("All Elasticsearch indices are ready");
            }
            else
            {
                _logger.LogWarning("Some Elasticsearch indices failed to create. Check logs for details.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Elasticsearch indices on startup");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}