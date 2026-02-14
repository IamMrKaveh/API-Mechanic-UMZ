namespace Application.Media.Features.Commands.CleanupOrphanedMedia;

public class CleanupOrphanedMediaHandler
    : IRequestHandler<CleanupOrphanedMediaCommand, ServiceResult<CleanupResultDto>>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IStorageService _storageService;
    private readonly ILogger<CleanupOrphanedMediaHandler> _logger;

    public CleanupOrphanedMediaHandler(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        ILogger<CleanupOrphanedMediaHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<ServiceResult<CleanupResultDto>> Handle(
        CleanupOrphanedMediaCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. لیست تمام فایل‌های فیزیکی
            var allFiles = await _storageService.GetFilesAsync(
                "uploads/", 1000, null, cancellationToken);

            if (!allFiles.Any())
                return ServiceResult<CleanupResultDto>.Success(new CleanupResultDto { DeletedFileCount = 0 });

            // 2. لیست تمام مسیرهای ثبت شده در دیتابیس
            var dbFilePaths = await _mediaRepository.GetAllFilePathsAsync(cancellationToken);

            // 3. شناسایی فایل‌های بدون رکورد
            var orphans = allFiles.Where(f => !dbFilePaths.Contains(f)).ToList();

            int deletedCount = 0;
            foreach (var orphan in orphans)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    await _storageService.DeleteFileAsync(orphan, cancellationToken);
                    deletedCount++;

                    _logger.LogInformation("فایل بلااستفاده حذف شد: {FilePath}", orphan);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطا در حذف فایل بلااستفاده: {FilePath}", orphan);
                }
            }

            _logger.LogInformation(
                "پاکسازی فایل‌های بلااستفاده: {Total} شناسایی شد، {Deleted} حذف شد.",
                orphans.Count, deletedCount);

            return ServiceResult<CleanupResultDto>.Success(new CleanupResultDto
            {
                DeletedFileCount = deletedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در پاکسازی فایل‌های بلااستفاده");
            return ServiceResult<CleanupResultDto>.Failure("خطا در پاکسازی فایل‌ها.");
        }
    }
}