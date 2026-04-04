using Application.Common.Results;
using Application.Media.Contracts;
using Domain.Media.Interfaces;

namespace Application.Media.Features.Commands.CleanupOrphanedMedia;

public class CleanupOrphanedMediaHandler(
    IMediaRepository mediaRepository,
    IStorageService storageService,
    ILogger<CleanupOrphanedMediaHandler> logger)
        : IRequestHandler<CleanupOrphanedMediaCommand, ServiceResult<CleanupResultDto>>
{
    private readonly IMediaRepository _mediaRepository = mediaRepository;
    private readonly IStorageService _storageService = storageService;
    private readonly ILogger<CleanupOrphanedMediaHandler> _logger = logger;

    public async Task<ServiceResult<CleanupResultDto>> Handle(
        CleanupOrphanedMediaCommand request,
        CancellationToken ct)
    {
        try
        {
            var allFiles = await _storageService.GetFilesAsync(
                "uploads/", 1000, null, ct);

            if (!allFiles.Any())
                return ServiceResult<CleanupResultDto>.Success(new CleanupResultDto { DeletedFileCount = 0 });

            var dbFilePaths = await _mediaRepository.GetAllFilePathsAsync(ct);

            var orphans = allFiles.Where(f => !dbFilePaths.Contains(f)).ToList();

            int deletedCount = 0;
            foreach (var orphan in orphans)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    await _storageService.DeleteFileAsync(orphan, ct);
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
            return ServiceResult<CleanupResultDto>.Unexpected("خطا در پاکسازی فایل‌ها.");
        }
    }
}