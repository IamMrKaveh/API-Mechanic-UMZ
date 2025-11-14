namespace Application.Services;

public class MediaService : IMediaService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IStorageService _storageService;
    private readonly ILogger<MediaService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;

    public MediaService(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        ILogger<MediaService> logger,
        ICacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _logger = logger;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Domain.Media.Media> AttachFileToEntityAsync(Stream stream, string fileName, string contentType, long contentLength, string entityType, int entityId, bool isPrimary, string? altText = null)
    {
        if (stream == null || contentLength == 0)
        {
            throw new ArgumentException("File stream is invalid.", nameof(stream));
        }

        var relativePath = await _storageService.UploadFileAsync(stream, fileName, contentType, $"images/{entityType.ToLower()}", entityId);

        var media = new Domain.Media.Media
        {
            FilePath = relativePath,
            FileName = fileName,
            FileType = contentType,
            FileSize = contentLength,
            EntityType = entityType,
            EntityId = entityId,
            IsPrimary = isPrimary,
            AltText = altText,
            CreatedAt = DateTime.UtcNow
        };

        if (isPrimary)
        {
            await _mediaRepository.UnsetPrimaryMediaAsync(entityType, entityId);
        }

        await _mediaRepository.AddMediaAsync(media);
        // Note: SaveChangesAsync is removed from here to be handled by the calling service (Unit of Work pattern)
        _logger.LogInformation("Prepared to attach new media to {EntityType} {EntityId}", entityType, entityId);

        if (entityType.Equals("Product", StringComparison.OrdinalIgnoreCase) || entityType.Equals("ProductVariant", StringComparison.OrdinalIgnoreCase))
        {
            await InvalidateCartsContainingProduct(entityId);
        }

        return media;
    }

    public async Task<List<Domain.Media.Media>> UploadFilesAsync(IEnumerable<(Stream stream, string fileName, string contentType, long contentLength)> files, string entityType, int entityId, bool isPrimary, string? altText)
    {
        var uploadedMedia = new List<Domain.Media.Media>();
        var firstFile = true;

        foreach (var (stream, fileName, contentType, contentLength) in files)
        {
            var media = await AttachFileToEntityAsync(stream, fileName, contentType, contentLength, entityType, entityId, isPrimary && firstFile, altText);
            uploadedMedia.Add(media);
            if (isPrimary) firstFile = false;
        }

        return uploadedMedia;
    }


    public async Task<IEnumerable<object>> GetMediaForEntityAsync(string entityType, int entityId)
    {
        var mediaItems = await _mediaRepository.GetMediaForEntityAsync(entityType, entityId);

        return mediaItems.Select(m => new
        {
            m.Id,
            m.AltText,
            m.IsPrimary,
            m.SortOrder,
            Url = _storageService.GetFileUrl(m.FilePath)
        });
    }

    public async Task<IEnumerable<Domain.Media.Media>> GetEntityMediaAsync(string entityType, int entityId)
    {
        return await _mediaRepository.GetMediaForEntityAsync(entityType, entityId);
    }

    public async Task<string?> GetPrimaryImageUrlAsync(string entityType, int entityId)
    {
        var filePath = await _mediaRepository.GetPrimaryMediaFilePathAsync(entityType, entityId);

        return filePath != null ? _storageService.GetFileUrl(filePath) : null;
    }

    public async Task<bool> SetPrimaryMediaAsync(int mediaId, int entityId, string entityType)
    {
        var mediaToSetAsPrimary = await _mediaRepository.GetMediaByIdAsync(mediaId);
        if (mediaToSetAsPrimary == null || mediaToSetAsPrimary.EntityId != entityId || mediaToSetAsPrimary.EntityType != entityType)
        {
            return false;
        }

        await _mediaRepository.UnsetPrimaryMediaAsync(entityType, entityId, mediaId);

        mediaToSetAsPrimary.IsPrimary = true;
        await _unitOfWork.SaveChangesAsync();

        if (entityType.Equals("Product", StringComparison.OrdinalIgnoreCase) || entityType.Equals("ProductVariant", StringComparison.OrdinalIgnoreCase))
        {
            await InvalidateCartsContainingProduct(entityId);
        }
        return true;
    }

    public async Task<bool> DeleteMediaAsync(int mediaId)
    {
        var media = await _mediaRepository.GetMediaByIdAsync(mediaId);
        if (media == null)
        {
            _logger.LogWarning("Attempted to delete non-existent media with ID {MediaId}", mediaId);
            return false;
        }

        try
        {
            await _storageService.DeleteFileAsync(media.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from storage for media {MediaId} at path {Path}", mediaId, media.FilePath);
        }

        _mediaRepository.DeleteMedia(media);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Deleted media with ID {MediaId}", mediaId);

        if (media.EntityType.Equals("Product", StringComparison.OrdinalIgnoreCase) || media.EntityType.Equals("ProductVariant", StringComparison.OrdinalIgnoreCase))
        {
            await InvalidateCartsContainingProduct(media.EntityId);
        }

        return true;
    }

    private async Task InvalidateCartsContainingProduct(int productId)
    {
        // This is a simplified invalidation. A more robust system might use a reverse index.
        // For now, we accept the inefficiency of clearing all carts as a trade-off.
        // A better approach would be to not include the image URL in the cart DTO itself.
        _logger.LogWarning("Product image changed for product {ProductId}. Invalidating all user carts in cache.", productId);
        await _cacheService.ClearByPrefixAsync("cart:user:");
        await _cacheService.ClearByPrefixAsync("cart:guest:");
    }
}