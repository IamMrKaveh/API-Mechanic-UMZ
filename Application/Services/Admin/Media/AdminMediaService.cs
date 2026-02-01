namespace Application.Services.Admin.Media;

public class AdminMediaService : IAdminMediaService
{
    private readonly IMediaRepository _mediaRepository; private readonly IStorageService _storageService; private readonly IUnitOfWork _unitOfWork; private readonly IAppLogger<AdminMediaService> _logger; private readonly IMapper _mapper;

    public AdminMediaService(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        IAppLogger<AdminMediaService> logger,
        IMapper mapper)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PagedResultDto<MediaDto>>> GetAllMediaAsync(int page, int pageSize, string? entityType = null)
    {
        var (items, totalItems) = await _mediaRepository.GetPagedAsync(page, pageSize, entityType);

        var dtos = _mapper.Map<List<MediaDto>>(items);

        foreach (var dto in dtos)
        {
            var media = items.First(m => m.Id == dto.Id);
            dto.Url = _storageService.GetUrl(media.FilePath);
        }

        return ServiceResult<PagedResultDto<MediaDto>>.Ok(new PagedResultDto<MediaDto>
        {
            Items = dtos,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult> DeleteMediaAsync(int id)
    {
        var media = await _mediaRepository.GetByIdAsync(id);
        if (media == null) return ServiceResult.Fail("Media not found");

        await _storageService.DeleteFileAsync(media.FilePath);
        _mediaRepository.Remove(media);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<(int Count, long Size)>> CleanupOrphanedMediaAsync()
    {
        try
        {
            var allFiles = new List<string>();
            var batch = await _storageService.ListFilesAsync("uploads/", 1000, null);
            allFiles.AddRange(batch);

            if (!allFiles.Any()) return ServiceResult<(int, long)>.Ok((0, 0));

            var fileKeys = allFiles.ToHashSet();
            var dbFiles = await _mediaRepository.GetAllFilePathsAsync(); // New repo method
            var dbFileSet = dbFiles.ToHashSet();

            var orphans = fileKeys.Where(k => !dbFileSet.Contains(k)).ToList();
            int deletedCount = 0;

            foreach (var orphan in orphans)
            {
                await _storageService.DeleteFileAsync(orphan);
                deletedCount++;
            }

            return ServiceResult<(int, long)>.Ok((deletedCount, 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleanup orphaned media");
            return ServiceResult<(int, long)>.Fail("Error during cleanup");
        }
    }
}