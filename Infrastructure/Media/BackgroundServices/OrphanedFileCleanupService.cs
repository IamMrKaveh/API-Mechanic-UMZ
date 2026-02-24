namespace Infrastructure.Media.BackgroundServices;

/// <summary>
/// scope در داخل حلقه while ایجاد می‌شود
/// این از بلوت‌شدن EF Core Change Tracker در طول اجرای طولانی جلوگیری می‌کند
/// </summary>
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
        _logger.LogInformation("Media Cleanup Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCleanupAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Media Cleanup Service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task ProcessCleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var context = scope.ServiceProvider.GetRequiredService<Persistence.Context.DBContext>();

        var cutoffDate = DateTime.UtcNow.AddHours(-24);

        var deletedMedias = await context.Medias
            .IgnoreQueryFilters()
            .Where(m => m.IsDeleted && m.DeletedAt < cutoffDate)
            .Take(100)
            .ToListAsync(stoppingToken);

        foreach (var media in deletedMedias)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                _logger.LogInformation("Deleting physical file for media: {Id}", media.Id);
                await storageService.DeleteFileAsync(media.FilePath);

                context.Medias.Remove(media);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FilePath}", media.FilePath);
            }
        }

        if (deletedMedias.Any())
        {
            await context.SaveChangesAsync(stoppingToken);
        }
    }
}