using Application.Common.Interfaces.Media;
using Application.DTOs.Media;

namespace Application.Services.Media;

public class MediaService : IMediaService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MediaService> _logger;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
    {
        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { ".webp", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 }, new byte[] { 0x57, 0x45, 0x42, 0x50 } } },
        { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }
    };

    public MediaService(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        ILogger<MediaService> logger,
        IMapper mapper,
        IConfiguration configuration)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<string?> GetPrimaryImageUrlAsync(string entityType, int entityId)
    {
        var primaryMedia = await _mediaRepository.GetPrimaryMediaByEntityAsync(entityType, entityId);
        if (primaryMedia != null)
        {
            return GetUrl(primaryMedia.FilePath);
        }

        var anyMedia = (await _mediaRepository.GetByEntityAsync(entityType, entityId)).FirstOrDefault();
        if (anyMedia != null)
        {
            return GetUrl(anyMedia.FilePath);
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
                dto.Url = GetUrl(media.FilePath);
            }
        }
        return dtos.OrderBy(m => m.SortOrder);
    }

    public async Task<Media> AttachFileToEntityAsync(Stream stream, string fileName, string contentType, long contentLength, string entityType, int entityId, bool isPrimary = false, string? altText = null, bool saveChanges = true)
    {
        ValidateFileType(stream, fileName);

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

        if (saveChanges)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        return media;
    }

    public async Task<IEnumerable<Media>> UploadFilesAsync(IEnumerable<(Stream stream, string fileName, string contentType, long length)> fileStreams, string entityType, int entityId, bool isPrimary = false, string? altText = null)
    {
        var uploadedMedia = new List<Media>();
        foreach (var (stream, fileName, contentType, length) in fileStreams)
        {
            var media = await AttachFileToEntityAsync(stream, fileName, contentType, length, entityType, entityId, isPrimary, altText, saveChanges: true);
            uploadedMedia.Add(media);
        }
        return uploadedMedia;
    }


    public async Task<bool> DeleteMediaAsync(int mediaId)
    {
        var media = await _mediaRepository.GetByIdAsync(mediaId);
        if (media == null)
            return false;

        media.IsDeleted = true;
        media.DeletedAt = DateTime.UtcNow;

        _mediaRepository.Update(media);

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

        var url = _storageService.GetUrl(filePath);
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = _configuration["LiaraStorage:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                return $"{baseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
            }
        }

        return url;
    }

    private void ValidateFileType(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !_fileSignatures.ContainsKey(ext))
        {
            throw new InvalidOperationException("File type not allowed.");
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
            var signatures = _fileSignatures[ext];
            var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

            bool isMatch = signatures.Any(signature =>
                headerBytes.Take(signature.Length).SequenceEqual(signature));

            stream.Position = 0;

            if (!isMatch)
            {
                throw new InvalidOperationException("Invalid file signature detected. Upload rejected.");
            }
        }
    }
}