using Application.Media.Features.Shared;
using Domain.Media.Interfaces;
using Domain.Media.Services;
using Domain.Media.ValueObjects;

namespace Infrastructure.Media.Services;

public sealed class MediaService(
    IMediaRepository mediaRepository,
    IStorageService storageService,
    IAuditService auditService,
    IUnitOfWork unitOfWork) : IMediaService
{
    public async Task<ServiceResult<MediaDto>> UploadAsync(
        Stream fileStream,
        FilePath filePath,
        FileSize fileSize,
        string entityType,
        Guid entityId,
        bool isPrimary = false,
        string? altText = null,
        CancellationToken ct = default)
    {
        var contentType = filePath.GetContentType();

        var storedPath = await storageService.UploadAsync(
            fileStream,
            filePath.FileName,
            contentType,
            null,
            ct);

        var existing = await mediaRepository.GetByEntityAsync(entityType, entityId, ct);
        var sortOrder = existing.Count;

        var media = Domain.Media.Aggregates.Media.Create(
            storedPath,
            filePath.FileName,
            contentType,
            fileSize.Bytes,
            entityType,
            entityId,
            sortOrder,
            isPrimary,
            altText);

        await mediaRepository.AddAsync(media, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "MediaUploaded",
            $"Media {media.Id.Value} uploaded for {entityType}/{entityId}", ct);

        return ServiceResult<MediaDto>.Success(new MediaDto
        {
            Id = media.Id.Value,
            FilePath = media.Path.Value,
            FileName = media.Path.FileName,
            FileType = media.FileType,
            FileSize = media.Size.Bytes,
            EntityType = media.EntityType,
            EntityId = media.EntityId,
            SortOrder = media.SortOrder,
            IsPrimary = media.IsPrimary,
            AltText = media.AltText,
            IsActive = media.IsActive,
            PublicUrl = storageService.GetPublicUrl(media.Path.Value),
            CreatedAt = media.CreatedAt
        });
    }

    public async Task<ServiceResult> DeleteAsync(
        MediaId mediaId,
        CancellationToken ct = default)
    {
        var media = await mediaRepository.GetByIdAsync(mediaId, ct);
        if (media is null)
            return ServiceResult.NotFound("رسانه یافت نشد.");

        var wasPrimary = media.IsPrimary;
        var entityType = media.EntityType;
        var entityId = media.EntityId;

        media.RequestDeletion();
        mediaRepository.Update(media);

        if (wasPrimary)
        {
            var remaining = await mediaRepository.GetByEntityAsync(entityType, entityId, ct);
            var newPrimary = MediaDomainService.SelectNewPrimaryAfterDeletion(
                remaining.Where(m => m.Id != mediaId).ToList());

            if (newPrimary is not null)
            {
                newPrimary.SetAsPrimary();
                mediaRepository.Update(newPrimary);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetAsPrimaryAsync(
        MediaId mediaId,
        CancellationToken ct = default)
    {
        var media = await mediaRepository.GetByIdAsync(mediaId, ct);
        if (media is null)
            return ServiceResult.NotFound("رسانه یافت نشد.");

        var currentPrimary = await mediaRepository.GetPrimaryByEntityAsync(
            media.EntityType, media.EntityId, ct);

        if (currentPrimary is not null && currentPrimary.Id != mediaId)
        {
            currentPrimary.RemovePrimary();
            mediaRepository.Update(currentPrimary);
        }

        media.SetAsPrimary();
        mediaRepository.Update(media);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ReorderAsync(
        string entityType,
        Guid entityId,
        ICollection<Guid> orderedIds,
        CancellationToken ct = default)
    {
        var medias = await mediaRepository.GetByEntityAsync(entityType, entityId, ct);

        var sortOrder = 0;
        foreach (var id in orderedIds)
        {
            var media = medias.FirstOrDefault(m => m.EntityId == id);
            if (media is null) continue;

            media.UpdateSortOrder(sortOrder++);
            mediaRepository.Update(media);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}