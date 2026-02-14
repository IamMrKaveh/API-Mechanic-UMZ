namespace Infrastructure.Search.BackgroundServices;

public class DeadLetterQueueProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeadLetterQueueProcessor> _logger;

    public DeadLetterQueueProcessor(
        IServiceProvider serviceProvider,
        ILogger<DeadLetterQueueProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Dead Letter Queue Processor is starting");

        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessFailedOperationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dead letter queue");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task ProcessFailedOperationsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dlq = scope.ServiceProvider.GetRequiredService<IElasticDeadLetterQueue>();
        var searchService = scope.ServiceProvider.GetRequiredService<ElasticSearchService>();
        var context = scope.ServiceProvider.GetRequiredService<LedkaContext>();

        var operations = await dlq.DequeueAsync(10, ct);

        foreach (var operation in operations)
        {
            try
            {
                _logger.LogInformation(
                    "Retrying failed operation: {EntityType} {EntityId}",
                    operation.EntityType,
                    operation.EntityId);

                switch (operation.EntityType)
                {
                    case "Product":
                        var product = JsonSerializer.Deserialize<ProductSearchDocument>(operation.Document);
                        if (product != null)
                            await searchService.IndexProductAsync(product, ct);
                        break;

                    case "Category":
                        var category = JsonSerializer.Deserialize<CategorySearchDocument>(operation.Document);
                        if (category != null)
                            await searchService.IndexCategoryAsync(category, ct);
                        break;

                    case "CategoryGroup":
                        var group = JsonSerializer.Deserialize<CategoryGroupSearchDocument>(operation.Document);
                        if (group != null)
                            await searchService.IndexCategoryGroupAsync(group, ct);
                        break;
                }

                var entity = await context.FailedElasticOperations
                    .FirstOrDefaultAsync(o =>
                        o.EntityType == operation.EntityType &&
                        o.EntityId == operation.EntityId &&
                        o.Status == "Pending", ct);

                if (entity != null)
                {
                    entity.Status = "Completed";
                    entity.LastRetryAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(ct);
                }

                _logger.LogInformation(
                    "Successfully processed failed operation: {EntityType} {EntityId}",
                    operation.EntityType,
                    operation.EntityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process operation after retry: {EntityType} {EntityId}",
                    operation.EntityType,
                    operation.EntityId);

                var entity = await context.FailedElasticOperations
                    .FirstOrDefaultAsync(o =>
                        o.EntityType == operation.EntityType &&
                        o.EntityId == operation.EntityId &&
                        o.Status == "Pending", ct);

                if (entity != null)
                {
                    entity.RetryCount++;
                    entity.LastRetryAt = DateTime.UtcNow;

                    if (entity.RetryCount >= 5)
                    {
                        entity.Status = "Failed";
                        _logger.LogError(
                            "Operation permanently failed after {RetryCount} retries: {EntityType} {EntityId}",
                            entity.RetryCount,
                            operation.EntityType,
                            operation.EntityId);
                    }

                    await context.SaveChangesAsync(ct);
                }
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Dead Letter Queue Processor is stopping");
        return base.StopAsync(cancellationToken);
    }
}