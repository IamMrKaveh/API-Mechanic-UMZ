using Application.Search.Features.Shared;
using Infrastructure.Search.Services;

namespace Infrastructure.BackgroundJobs;

public sealed class DeadLetterQueueJob(
    IServiceProvider serviceProvider,
    IAuditService auditService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await auditService.LogInformationAsync("Dead Letter Queue Processor is starting", ct);
        await Task.Delay(TimeSpan.FromMinutes(1), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessFailedOperationsAsync(ct);
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(
                    $"Error processing dead letter queue: {ex.Message}", ct);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }

    private async Task ProcessFailedOperationsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dlq = scope.ServiceProvider.GetRequiredService<IElasticDeadLetterQueue>();
        var searchService = scope.ServiceProvider.GetRequiredService<ElasticsearchService>();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();

        var operations = await dlq.DequeueAsync(10, ct);

        foreach (var operation in operations)
        {
            try
            {
                await auditService.LogInformationAsync(
                    $"Retrying failed operation: {operation.EntityType} {operation.EntityId}", ct);

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

                    case "Brand":
                        var brand = JsonSerializer.Deserialize<BrandSearchDocument>(operation.Document);
                        if (brand != null)
                            await searchService.IndexBrandAsync(brand, ct);
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

                await auditService.LogInformationAsync(
                    $"Successfully processed failed operation: {operation.EntityType} {operation.EntityId}", ct);
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(
                    $"Failed to process operation after retry: {operation.EntityType} {operation.EntityId}: {ex.Message}", ct);

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
                        await auditService.LogErrorAsync(
                            $"Operation permanently failed after {entity.RetryCount} retries: {operation.EntityType} {operation.EntityId}", ct);
                    }

                    await context.SaveChangesAsync(ct);
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        await auditService.LogInformationAsync("Dead Letter Queue Processor is stopping", ct);
        await base.StopAsync(ct);
    }
}