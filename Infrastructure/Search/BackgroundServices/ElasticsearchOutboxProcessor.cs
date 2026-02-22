namespace Infrastructure.Search.BackgroundServices;

public class ElasticsearchOutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ElasticsearchOutboxProcessor> _logger;
    private readonly IConfiguration _configuration;

    public ElasticsearchOutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<ElasticsearchOutboxProcessor> logger,
        IConfiguration configuration
        )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
        )
    {
        var intervalSeconds = _configuration.GetValue("Elasticsearch:DeadLetterQueue:ProcessIntervalSeconds", 60);
        var maxRetries = _configuration.GetValue("Elasticsearch:DeadLetterQueue:MaxRetries", 5);
        var batchSize = _configuration.GetValue("Elasticsearch:Sync:BatchSize", 100);

        _logger.LogInformation(
            "Elasticsearch outbox processor started. Interval: {Interval}s, MaxRetries: {MaxRetries}",
            intervalSeconds, maxRetries);

        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(batchSize, maxRetries, stoppingToken);
                await ProcessDeadLetterQueueAsync(batchSize, maxRetries, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Elasticsearch outbox processor is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Elasticsearch outbox processor");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken ct
        )
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        var messages = await dbContext.ElasticsearchOutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < maxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (!messages.Any()) return;

        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await ProcessSingleMessageAsync(message, searchService, ct);
                message.ProcessedAt = DateTime.UtcNow;
                _logger.LogDebug("Outbox message processed: {EntityType} {EntityId}", message.EntityType, message.EntityId);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogWarning(ex,
                    "Failed to process outbox message: {EntityType} {EntityId}. Retry: {RetryCount}",
                    message.EntityType, message.EntityId, message.RetryCount);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task ProcessSingleMessageAsync(
        ElasticsearchOutboxMessage message,
        ISearchService searchService,
        CancellationToken ct
        )
    {
        switch (message.EntityType)
        {
            case "Product":
                var productDoc = System.Text.Json.JsonSerializer.Deserialize<ProductSearchDocument>(message.Document!);
                if (productDoc != null) await searchService.IndexProductAsync(productDoc, ct);
                break;

            case "Category":
                var categoryDoc = System.Text.Json.JsonSerializer.Deserialize<CategorySearchDocument>(message.Document!);
                if (categoryDoc != null) await searchService.IndexCategoryAsync(categoryDoc, ct);
                break;

            case "Brand":
                var groupDoc = System.Text.Json.JsonSerializer.Deserialize<BrandSearchDocument>(message.Document!);
                if (groupDoc != null) await searchService.IndexBrandAsync(groupDoc, ct);
                break;
        }
    }

    private async Task ProcessDeadLetterQueueAsync(
        int batchSize,
        int maxRetries,
        CancellationToken ct
        )
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        var retryBackoffMultiplier = _configuration.GetValue("Elasticsearch:DeadLetterQueue:RetryBackoffMultiplier", 2);

        var failedOps = await dbContext.FailedElasticOperations
            .Where(o => o.Status == "Pending" && o.RetryCount < maxRetries)
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (!failedOps.Any()) return;

        _logger.LogInformation("Processing {Count} dead letter queue items", failedOps.Count);

        foreach (var op in failedOps)
        {
            if (op.LastRetryAt.HasValue)
            {
                var backoffSeconds = Math.Pow(retryBackoffMultiplier, op.RetryCount);
                var nextRetryAt = op.LastRetryAt.Value.AddSeconds(backoffSeconds);
                if (DateTime.UtcNow < nextRetryAt) continue;
            }

            try
            {
                var outboxMessage = new ElasticsearchOutboxMessage
                {
                    EntityType = op.EntityType,
                    EntityId = op.EntityId,
                    Document = op.Document,
                    ChangeType = "Retry",
                    CreatedAt = DateTime.UtcNow
                };

                await ProcessSingleMessageAsync(outboxMessage, searchService, ct);

                op.Status = "Processed";
                op.LastRetryAt = DateTime.UtcNow;

                _logger.LogInformation("DLQ item processed successfully: {EntityType} {EntityId}", op.EntityType, op.EntityId);
            }
            catch (Exception ex)
            {
                op.RetryCount++;
                op.LastRetryAt = DateTime.UtcNow;
                op.Error = ex.Message;

                if (op.RetryCount >= maxRetries)
                {
                    op.Status = "Failed";
                    _logger.LogError("DLQ item permanently failed after {MaxRetries} retries: {EntityType} {EntityId}", maxRetries, op.EntityType, op.EntityId);
                }
                else
                {
                    _logger.LogWarning(ex, "DLQ item retry failed: {EntityType} {EntityId}. Retry: {RetryCount}/{MaxRetries}", op.EntityType, op.EntityId, op.RetryCount, maxRetries);
                }
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}