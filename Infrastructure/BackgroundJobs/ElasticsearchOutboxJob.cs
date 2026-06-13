using Infrastructure.Search.Contracts;

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
        var indexer = scope.ServiceProvider.GetRequiredService<IElasticsearchIndexer>();

        var now = DateTime.UtcNow;

        var messages = await dbContext.ElasticsearchOutboxMessages
            .Where(m => m.ProcessedAt == null
                        && !m.IsPoisoned
                        && m.RetryCount < maxRetries
                        && (m.NextAttemptAt == null || m.NextAttemptAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        await auditService.LogInformationAsync($"Processing {messages.Count} ES outbox messages", ct);

        foreach (var message in messages)
        {
            try
            {
                var alreadyProcessed = await dbContext.ElasticsearchOutboxMessages
                    .AnyAsync(m => m.IdempotencyKey == message.IdempotencyKey
                                   && m.Id != message.Id
                                   && m.ProcessedAt != null
                                   && m.CreatedAt > message.CreatedAt, ct);

                if (alreadyProcessed)
                {
                    message.MarkProcessed();
                    continue;
                }

                var success = await indexer.IndexDocumentAsync(
                    message.EntityType,
                    message.EntityId,
                    message.Document,
                    message.ChangeType,
                    ct);

                if (success)
                {
                    message.MarkProcessed();
                }
                else
                {
                    var delay = ComputeRetryDelay(message.RetryCount);
                    message.MarkFailed("Indexing returned non-success response.", delay);

                    if (message.RetryCount >= maxRetries)
                        message.MarkPoisoned($"Exceeded max retries ({maxRetries}).");
                }
            }
            catch (Exception ex)
            {
                var delay = ComputeRetryDelay(message.RetryCount);
                message.MarkFailed(ex.Message, delay);

                if (message.RetryCount >= maxRetries)
                    message.MarkPoisoned($"Exceeded max retries ({maxRetries}): {ex.Message}");

                await auditService.LogErrorAsync(
                    $"Failed to process ES outbox message {message.Id}: {ex.Message}", ct);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task ProcessDeadLetterQueueAsync(int batchSize, int maxRetries, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
        var indexer = scope.ServiceProvider.GetRequiredService<IElasticsearchIndexer>();

        var failedOps = await dbContext.FailedElasticOperations
            .Where(o => o.Status == "Pending" && o.RetryCount < maxRetries)
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (failedOps.Count == 0) return;

        await auditService.LogInformationAsync(
            $"Processing {failedOps.Count} dead letter queue items", ct);

        foreach (var op in failedOps)
        {
            try
            {
                if (!Guid.TryParse(op.EntityId, out var entityId))
                {
                    op.Status = "Poisoned";
                    op.Error = $"Invalid entity id: {op.EntityId}";
                    continue;
                }

                var success = await indexer.IndexDocumentAsync(
                    op.EntityType,
                    entityId,
                    op.Document,
                    op.EntityType ?? "Update",
                    ct);

                if (success)
                {
                    op.Status = "Recovered";
                    op.Error = "";
                }
                else
                {
                    op.RetryCount++;
                    op.Error = "Recovery indexing returned non-success response.";
                    if (op.RetryCount >= maxRetries)
                        op.Status = "Poisoned";
                }
            }
            catch (Exception ex)
            {
                op.RetryCount++;
                op.Error = ex.Message;
                if (op.RetryCount >= maxRetries)
                    op.Status = "Poisoned";

                await auditService.LogErrorAsync(
                    $"Failed to recover dead-lettered ES operation {op.Id}: {ex.Message}", ct);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static TimeSpan ComputeRetryDelay(int retryCount)
    {
        var seconds = Math.Min(300, Math.Pow(2, retryCount + 1));
        return TimeSpan.FromSeconds(seconds);
    }
}