using DataAccessLayer.Models.Media;

namespace MainApi.Services.Media;

public class MediaService : IMediaService
{
    private readonly MechanicContext _context;
    private readonly IStorageService _storageService;
    private readonly ILogger<MediaService> _logger;
    private readonly ICacheService _cacheService;

    public MediaService(MechanicContext context, IStorageService storageService, ILogger<MediaService> logger, ICacheService cacheService)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<TMedia> AttachFileToEntityAsync(IFormFile file, string entityType, int entityId, bool isPrimary, string? altText = null)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null.", nameof(file));
        }

        var relativePath = await _storageService.UploadFileAsync(file, $"images/{entityType.ToLower()}", entityId);

        var media = new TMedia
        {
            FilePath = relativePath,
            FileName = file.FileName,
            FileType = file.ContentType,
            FileSize = file.Length,
            EntityType = entityType,
            EntityId = entityId,
            IsPrimary = isPrimary,
            AltText = altText,
            CreatedAt = DateTime.UtcNow
        };

        if (isPrimary)
        {
            var existingPrimary = await _context.TMedia
                .FirstOrDefaultAsync(m => m.EntityType == entityType && m.EntityId == entityId && m.IsPrimary);
            if (existingPrimary != null)
            {
                existingPrimary.IsPrimary = false;
            }
        }

        _context.TMedia.Add(media);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Attached new media {MediaId} to {EntityType} {EntityId}", media.Id, entityType, entityId);

        if (entityType.Equals("Product", StringComparison.OrdinalIgnoreCase) || entityType.Equals("ProductVariant", StringComparison.OrdinalIgnoreCase))
        {
            await _cacheService.ClearByPrefixAsync("cart:user:");
        }

        return media;
    }

    public async Task<IEnumerable<TMedia>> GetEntityMediaAsync(string entityType, int entityId)
    {
        return await _context.TMedia
            .Where(m => m.EntityType == entityType && m.EntityId == entityId)
            .OrderBy(m => m.SortOrder)
            .ThenByDescending(m => m.IsPrimary)
            .ToListAsync();
    }

    public async Task<string?> GetPrimaryImageUrlAsync(string entityType, int entityId)
    {
        return await _context.TMedia
            .Where(m => m.EntityType == entityType && m.EntityId == entityId && m.IsPrimary)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SetPrimaryMediaAsync(int mediaId, int entityId, string entityType)
    {
        var mediaToSetAsPrimary = await _context.TMedia.FindAsync(mediaId);
        if (mediaToSetAsPrimary == null || mediaToSetAsPrimary.EntityId != entityId || mediaToSetAsPrimary.EntityType != entityType)
        {
            return false;
        }

        var currentPrimary = await _context.TMedia
            .Where(m => m.EntityId == entityId && m.EntityType == entityType && m.IsPrimary && m.Id != mediaId)
            .ToListAsync();

        foreach (var item in currentPrimary)
        {
            item.IsPrimary = false;
        }

        mediaToSetAsPrimary.IsPrimary = true;
        await _context.SaveChangesAsync();

        if (entityType.Equals("Product", StringComparison.OrdinalIgnoreCase) || entityType.Equals("ProductVariant", StringComparison.OrdinalIgnoreCase))
        {
            await _cacheService.ClearByPrefixAsync("cart:user:");
        }
        return true;
    }

    public async Task<bool> DeleteMediaAsync(int mediaId)
    {
        var media = await _context.TMedia.FindAsync(mediaId);
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

        _context.TMedia.Remove(media);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted media with ID {MediaId}", mediaId);

        if (media.EntityType.Equals("Product", StringComparison.OrdinalIgnoreCase) || media.EntityType.Equals("ProductVariant", StringComparison.OrdinalIgnoreCase))
        {
            await _cacheService.ClearByPrefixAsync("cart:user:");
        }

        return true;
    }
}