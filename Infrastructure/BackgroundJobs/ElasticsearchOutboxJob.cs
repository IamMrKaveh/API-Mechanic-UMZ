namespace Infrastructure.BackgroundJobs;

public sealed class ElasticsearchOutboxJob(
    IServiceScopeFactory scopeFactory,
    IAuditService auditService,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var intervalSeconds = configuration.GetValue("Elasticsearch:DeadLetterQueue:ProcessIntervalSeconds", 60);
        var maxRetries = configuration.GetValue("Elasticsearch:DeadLetterQueue:MaxRetries", 5);
        var batchSize = configuration.GetValue("Elasticsearch:Sync:BatchSize", 100);

        await auditService.LogInformationAsync(
            $"Elasticsearch outbox processor started. Interval: {intervalSeconds}s, MaxRetries: {maxRetries}", ct);

        await Task.Delay(TimeSpan.FromSeconds(15), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(batchSize, maxRetries, ct);
                await ProcessDeadLetterQueueAsync(batchSize, maxRetries, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                await auditService.LogInformationAsync("Elasticsearch outbox processor is stopping", ct);
                break;
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(
                    $"Error in Elasticsearch outbox processor: {ex.Message}", ct);
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
        }
    }

    private async Task ProcessOutboxMessagesAsync(int batchSize, int maxRetries, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        var messages = await dbContext.ElasticsearchOutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < maxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (!messages.Any()) return;

        await auditService.LogInformationAsync($"Processing {messages.Count} outbox messages", ct);

        foreach (var message in messages)
        {
            try
            {
                message.MarkProcessed();
            }
            catch (Exception ex)
            {
                message.IncrementRetry(ex.Message);
                await auditService.LogErrorAsync(
                    $"Failed to process outbox message {message.Id}: {ex.Message}", ct);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task ProcessDeadLetterQueueAsync(int batchSize, int maxRetries, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

        var failedOps = await dbContext.FailedElasticOperations
            .Where(o => o.Status == "Pending" && o.RetryCount < maxRetries)
            .Take(batchSize)
            .ToListAsync(ct);

        if (!failedOps.Any()) return;

        await auditService.LogInformationAsync(
            $"Processing {failedOps.Count} dead letter queue items", ct);

        await dbContext.SaveChangesAsync(ct);
    }
}