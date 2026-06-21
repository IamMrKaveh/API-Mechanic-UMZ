using Infrastructure.Search.Options;

namespace Infrastructure.BackgroundJobs;

public sealed class ElasticsearchSyncJob(
    IServiceProvider serviceProvider,
    IAuditService auditService,
    IDistributedLock distributedLock,
    IOptions<ElasticsearchOptions> options) : BackgroundService
{
    private readonly ElasticsearchOptions _options = options.Value;
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (_options.IsEnabled is false)
        {
            await auditService.LogInformationAsync("Elasticsearch sync is disabled", ct);
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(30), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:elasticsearch-sync", LockExpiry, ct);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    using var scope = serviceProvider.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<ISearchDatabaseSyncService>();
                    await syncService.SyncAsync(ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(
                    $"Error in Elasticsearch sync background service: {ex.Message}", ct);
            }

            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }
}