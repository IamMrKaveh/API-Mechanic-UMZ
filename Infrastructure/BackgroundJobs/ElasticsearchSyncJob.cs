using Infrastructure.Search.Options;

namespace Infrastructure.BackgroundJobs;

public sealed class ElasticsearchSyncJob(
    IServiceProvider serviceProvider,
    IAuditService auditService,
    IOptions<ElasticsearchOptions> options) : BackgroundService
{
    private readonly ElasticsearchOptions _options = options.Value;

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
                using var scope = serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ISearchDatabaseSyncService>();
                await syncService.SyncAsync(ct);
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