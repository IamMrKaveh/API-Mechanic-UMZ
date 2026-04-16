namespace Infrastructure.BackgroundJobs;

public sealed class OrphanedFileCleanupJob(
    IServiceProvider serviceProvider,
    IAuditService auditService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await auditService.LogInformationAsync("Media Cleanup Service started.", ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessCleanupAsync(ct);
                await Task.Delay(TimeSpan.FromHours(12), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync($"Media cleanup error: {ex.Message}", ct);
                await Task.Delay(TimeSpan.FromHours(1), ct);
            }
        }

        await auditService.LogInformationAsync("Media Cleanup Service stopped.", ct);
    }

    private async Task ProcessCleanupAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var context = scope.ServiceProvider.GetRequiredService<DBContext>();

        var cutoffDate = DateTime.UtcNow.AddHours(-24);

        var deletedMedias = await context.Medias
            .IgnoreQueryFilters()
            .Where(m => m.IsDeleted && m.DeletedAt < cutoffDate)
            .Take(100)
            .ToListAsync(ct);

        foreach (var media in deletedMedias)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await storageService.DeleteAsync(media.Path.Value, ct);
                context.Medias.Remove(media);
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(
                    $"Failed to delete file {media.Path.Value}: {ex.Message}", ct);
            }
        }

        if (deletedMedias.Count > 0)
            await context.SaveChangesAsync(ct);
    }
}