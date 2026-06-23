using Application.Media.Contracts;
using Domain.Media.Interfaces;

namespace Application.Media.Features.Commands.CleanupOrphanedMedia;

public class CleanupOrphanedMediaHandler(
    IMediaRepository mediaRepository,
    IStorageService storageService,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : ICommandHandler<CleanupOrphanedMediaCommand, int>
{
    public async Task<ServiceResult<int>> Handle(
        CleanupOrphanedMediaCommand request,
        CancellationToken ct)
    {
        var allPaths = await mediaRepository.GetAllFilePathsAsync(ct);
        var deletedCount = 0;

        foreach (var path in allPaths)
        {
            if (ct.IsCancellationRequested) break;

            var existsInStorage = await storageService.ExistsAsync(path, ct);
            if (existsInStorage) continue;

            var orphans = await mediaRepository.GetByPathAsync(path, ct);
            foreach (var orphan in orphans)
            {
                orphan.RequestDeletion();
                mediaRepository.Update(orphan);
                deletedCount++;
            }
        }

        if (deletedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
            await auditService.LogSystemEventAsync(
                "OrphanedMediaCleanup",
                $"{deletedCount} orphaned media record(s) marked for deletion.",
                ct);
        }

        return ServiceResult<int>.Success(deletedCount);
    }
}