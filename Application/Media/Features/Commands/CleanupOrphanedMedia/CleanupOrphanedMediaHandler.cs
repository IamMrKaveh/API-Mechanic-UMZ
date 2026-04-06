using Application.Common.Results;
using Application.Media.Contracts;
using Domain.Media.Interfaces;

namespace Application.Media.Features.Commands.CleanupOrphanedMedia;

public class CleanupOrphanedMediaHandler(
    IMediaRepository mediaRepository,
    IStorageService storageService,
    ILogger<CleanupOrphanedMediaHandler> logger) : IRequestHandler<CleanupOrphanedMediaCommand, ServiceResult<int>>
{
    public async Task<ServiceResult<int>> Handle(CleanupOrphanedMediaCommand request, CancellationToken ct)
    {
        var allPaths = await mediaRepository.GetAllFilePathsAsync(ct);
        var deletedCount = 0;

        foreach (var path in allPaths)
        {
            if (!await storageService.ExistsAsync(path, ct))
            {
                logger.LogWarning("Orphaned media path: {Path}", path);
                deletedCount++;
            }
        }

        logger.LogInformation("Found {Count} orphaned media files", deletedCount);
        return ServiceResult<int>.Success(deletedCount);
    }
}