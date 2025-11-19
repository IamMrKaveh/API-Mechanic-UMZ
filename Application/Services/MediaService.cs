namespace Application.Services;

public class MediaService : IMediaService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MediaService> _logger;
    private readonly IMapper _mapper;

    public MediaService(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        ILogger<MediaService> logger,
        IMapper mapper)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<string?> GetPrimaryImageUrlAsync(string entityType, int entityId)
    {
        var primaryMedia = await _mediaRepository.GetPrimaryMediaByEntityAsync(entityType, entityId);
        if (primaryMedia != null)
        {
            return _storageService.GetUrl(primaryMedia.FilePath);
        }

        var anyMedia = (await _mediaRepository.GetByEntityAsync(entityType, entityId)).FirstOrDefault();
        if (anyMedia != null)
        {
            return _storageService.GetUrl(anyMedia.FilePath);
        }

        return string.Empty;
    }

    public async Task<IEnumerable<MediaDto>> GetEntityMediaAsync(string entityType, int entityId)
    {
        var mediaItems = await _mediaRepository.GetByEntityAsync(entityType, entityId);
        var dtos = _mapper.Map<IEnumerable<MediaDto>>(mediaItems).ToList();
        foreach (var dto in dtos)
        {
            var media = mediaItems.FirstOrDefault(m => m.Id == dto.Id);
            if (media != null)
            {
                dto.Url = _storageService.GetUrl(media.FilePath);
            }
        }
        return dtos.OrderBy(m => m.SortOrder);
    }

    public async Task<Media> AttachFileToEntityAsync(Stream stream, string fileName, string contentType, long contentLength, string entityType, int entityId, bool isPrimary, string? altText = null)
    {
        var (filePath, uniqueFileName) = await _storageService.SaveFileAsync(stream, fileName, entityType, entityId.ToString());
        var media = new Media
        {
            FileName = uniqueFileName,
            FilePath = filePath,
            FileType = contentType,
            FileSize = contentLength,
            EntityType = entityType,
            EntityId = entityId,
            IsPrimary = isPrimary,
            AltText = altText
        };
        await _mediaRepository.AddAsync(media);
        await _unitOfWork.SaveChangesAsync();
        return media;
    }

    public async Task<IEnumerable<Media>> UploadFilesAsync(IEnumerable<(Stream stream, string fileName, string contentType, long length)> fileStreams, string entityType, int entityId, bool isPrimary = false, string? altText = null)
    {
        var uploadedMedia = new List<Media>();
        foreach (var (stream, fileName, contentType, length) in fileStreams)
        {
            var media = await AttachFileToEntityAsync(stream, fileName, contentType, length, entityType, entityId, isPrimary, altText);
            uploadedMedia.Add(media);
        }
        return uploadedMedia;
    }


    public async Task<bool> DeleteMediaAsync(int mediaId)
    {
        var media = await _mediaRepository.GetByIdAsync(mediaId);
        if (media == null) return false;

        await _storageService.DeleteFileAsync(media.FilePath);
        _mediaRepository.Remove(media);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetPrimaryMediaAsync(int mediaId, int entityId, string entityType)
    {
        var allMedia = await _mediaRepository.GetByEntityAsync(entityType, entityId);
        bool found = false;
        foreach (var m in allMedia)
        {
            if (m.Id == mediaId)
            {
                m.IsPrimary = true;
                found = true;
            }
            else
            {
                m.IsPrimary = false;
            }
        }

        if (!found) return false;

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public string GetUrl(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;

        return _storageService.GetUrl(filePath);
    }
}