namespace Infrastructure.Media.Services;

public class MediaService : IMediaService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IStorageService _storageService;
    private readonly MediaDomainService _mediaDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        MediaDomainService mediaDomainService,
        IUnitOfWork unitOfWork,
        ILogger<MediaService> logger)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _mediaDomainService = mediaDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Domain.Media.Media> AttachFileToEntityAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string entityType,
        int entityId,
        bool isPrimary = false,
        string? altText = null,
        bool saveChanges = true,
        CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.');
        var (isValidType, typeError) = _mediaDomainService.ValidateFileTypeForEntity(entityType, extension);
        if (!isValidType)
            throw new DomainException(typeError!);

        var existingMedias = await _mediaRepository.GetByEntityAsync(entityType, entityId, ct);
        var (canAdd, addError) = _mediaDomainService.ValidateAddMedia(existingMedias, contentType);
        if (!canAdd)
            throw new DomainException(addError!);

        var directory = $"uploads/{entityType.ToLowerInvariant()}/{entityId}";
        var filePath = await _storageService.UploadFileAsync(
            fileStream, fileName, contentType, directory, ct);

        try
        {
            var shouldBePrimary = isPrimary || !existingMedias.Any();
            var media = Domain.Media.Media.Create(
                filePath, fileName, contentType, fileSize,
                entityType, entityId,
                sortOrder: existingMedias.Count,
                isPrimary: shouldBePrimary,
                altText: altText);

            if (shouldBePrimary)
            {
                foreach (var existing in existingMedias.Where(m => m.IsPrimary))
                {
                    existing.RemovePrimary();
                    _mediaRepository.Update(existing);
                }
            }

            await _mediaRepository.AddAsync(media, ct);

            if (saveChanges)
                await _unitOfWork.SaveChangesAsync(ct);

            return media;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در ذخیره رسانه. حذف فایل آپلود شده: {FilePath}", filePath);
            try
            {
                await _storageService.DeleteFileAsync(filePath, ct);
            }
            catch (Exception deleteEx)
            {
                _logger.LogError(deleteEx, "خطا در حذف فایل بعد از Rollback");
            }
            throw;
        }
    }

    public async Task DeleteMediaAsync(
        int mediaId, int? deletedBy = null, CancellationToken ct = default)
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