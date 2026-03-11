using Domain.Media.Interfaces;

namespace Infrastructure.Media.Services;

public class MediaService(
    IMediaRepository mediaRepository,
    IStorageService storageService,
    MediaDomainService mediaDomainService,
    IUnitOfWork unitOfWork,
    ILogger<MediaService> logger) : IMediaService
{
    private readonly IMediaRepository _mediaRepository = mediaRepository;
    private readonly IStorageService _storageService = storageService;
    private readonly MediaDomainService _mediaDomainService = mediaDomainService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<MediaService> _logger = logger;

    public async Task<MediaFile> AttachFileToEntityAsync(
        int entityId,
        string entityType,
        IFormFile file,
        CancellationToken ct)
    {
        var filePath = await _storageService.UploadFileAsync(
            file,
            file.FileName,
            file.ContentType,
            "directory",
            ct);

        var mediaFile = MediaFile.Create(entityId, entityType, filePath, file.ContentType, file.FileName);

        await _mediaRepository.AddAsync(mediaFile, ct);

        return mediaFile;
    }

    public async Task DeleteMediaAsync(
        int mediaId,
        int? deletedBy = null,
        CancellationToken ct = default)
    {
        var media = await _mediaRepository.GetByIdAsync(mediaId, ct);
        if (media == null) return;

        var wasPrimary = media.IsPrimary;
        var entityType = media.EntityType;
        var entityId = media.EntityId;

        media.Delete(deletedBy);
        _mediaRepository.Update(media);

        if (wasPrimary)
        {
            var remainingMedias = await _mediaRepository.GetByEntityAsync(entityType, entityId, ct);
            var remaining = remainingMedias.Where(m => m.Id != mediaId).ToList();
            var newPrimary = _mediaDomainService.SelectNewPrimaryAfterDeletion(remaining);

            if (newPrimary != null)
            {
                newPrimary.SetAsPrimary();
                _mediaRepository.Update(newPrimary);
            }
        }
    }
}