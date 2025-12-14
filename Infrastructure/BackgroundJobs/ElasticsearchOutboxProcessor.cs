namespace Infrastructure.BackgroundJobs;

public class ElasticsearchOutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ElasticsearchOutboxProcessor> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    public ElasticsearchOutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<ElasticsearchOutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LedkaContext>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        // دریافت پیام‌های unprocessed
        var messages = await context.ElasticsearchOutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                // پردازش پیام
                switch (message.EntityType)
                {
                    case "Product":
                        var document = JsonSerializer.Deserialize<ProductSearchDocument>(message.Document);
                        if (document != null)
                        {
                            if (message.ChangeType == "Deleted")
                            {
                                // حذف از index
                            }
                            else
                            {
                                await searchService.IndexProductAsync(document, ct);
                            }
                        }
                        break;
                }

                // ✅ علامت‌گذاری به عنوان processed
                message.ProcessedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Processed outbox message {MessageId} for {EntityType} {EntityId}",
                    message.Id, message.EntityType, message.EntityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process outbox message {MessageId}. Retry count: {RetryCount}",
                    message.Id, message.RetryCount);

                message.RetryCount++;
                message.Error = ex.Message;
                await context.SaveChangesAsync(ct);
            }
        }
    }
}