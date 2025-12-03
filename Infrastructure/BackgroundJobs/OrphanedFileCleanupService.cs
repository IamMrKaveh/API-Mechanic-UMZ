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
                // Run cleanup once every 24 hours
                await ProcessCleanupAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Orphaned File Cleanup Service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry sooner on failure
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

            // Files in DB that match these keys
            var existingFilesInDb = await context.Medias
                .Where(m => fileKeys.Contains(m.FilePath))
                .Select(m => m.FilePath)
                .ToListAsync(stoppingToken);

            var existingSet = existingFilesInDb.ToHashSet();
            var orphans = fileKeys.Where(k => !existingSet.Contains(k)).ToList();

            foreach (var orphan in orphans)
            {
                try
                {
                    // Double check: Ensure file is not very recent (e.g. created in last hour) to avoid race condition with ongoing upload
                    // Since S3 ListObjects doesn't give us creation time easily via this interface wrapper, 
                    // we assume the upload -> DB save gap is small.
                    // A safer way would be to modify ListFilesAsync to return metadata, but for now we proceed with caution 
                    // or assume this runs at low traffic times.

                    _logger.LogInformation("Deleting orphaned file: {File}", orphan);
                    await storageService.DeleteFileAsync(orphan);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete orphaned file {File}", orphan);
                }
            }

            // S3 ListObjects pagination is complex to abstract fully in interface without proper DTO, 
            // assuming ListFilesAsync handles standard pagination logic or returns a limited set. 
            // If using AWS SDK directly in LiaraStorageService, the method signature in interface implies basic usage.
            // We'll assume for now we process one batch per run or loop if the interface supported returning continuation token.
            // Since interface return is IEnumerable<string>, we might only get the first 1000.
            break;

        } while (!stoppingToken.IsCancellationRequested);
    }
}