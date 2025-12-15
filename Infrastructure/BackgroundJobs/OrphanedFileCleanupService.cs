namespace Infrastructure.BackgroundJobs;

public class OrphanedFileCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrphanedFileCleanupService> _logger;

    public OrphanedFileCleanupService(IServiceProvider serviceProvider, ILogger<OrphanedFileCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Orphaned File Cleanup Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCleanupAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Orphaned File Cleanup Service stopping.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Orphaned File Cleanup Service");
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task ProcessCleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var context = scope.ServiceProvider.GetRequiredService<LedkaContext>();

        string? continuationToken = null;

        do
        {
            var files = (await storageService.ListFilesAsync("uploads/", 1000, continuationToken)).ToList();
            if (!files.Any()) break;

            var fileKeys = files.ToHashSet();

            var existingFilesInDb = await context.Medias
                .Where(m => fileKeys.Contains(m.FilePath))
                .Select(m => m.FilePath)
                .ToListAsync(stoppingToken);

            var existingSet = existingFilesInDb.ToHashSet();
            var orphans = fileKeys.Where(k => !existingSet.Contains(k)).ToList();

            foreach (var orphan in orphans)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    _logger.LogInformation("Deleting orphaned file: {File}", orphan);
                    await storageService.DeleteFileAsync(orphan);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete orphaned file {File}", orphan);
                }
            }

            break;

        } while (!stoppingToken.IsCancellationRequested);
    }
}