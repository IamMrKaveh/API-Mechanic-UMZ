namespace Application.Media.Features.Commands.UploadMedia;

public class UploadMediaHandler : IRequestHandler<UploadMediaCommand, ServiceResult<MediaDto>>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IStorageService _storageService;
    private readonly IMediaQueryService _mediaQueryService;
    private readonly MediaDomainService _mediaDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadMediaHandler> _logger;

    public UploadMediaHandler(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        IMediaQueryService mediaQueryService,
        MediaDomainService mediaDomainService,
        IUnitOfWork unitOfWork,
        ILogger<UploadMediaHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _mediaQueryService = mediaQueryService;
        _mediaDomainService = mediaDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<MediaDto>> Handle(
        UploadMediaCommand request, CancellationToken cancellationToken)
    {
        
        var extension = Path.GetExtension(request.FileName).TrimStart('.');
        var (isValidType, typeError) = _mediaDomainService.ValidateFileTypeForEntity(
            request.EntityType, extension);

        if (!isValidType)
            return ServiceResult<MediaDto>.Failure(typeError!);

        
        var existingMedias = await _mediaRepository.GetByEntityAsync(
            request.EntityType, request.EntityId, cancellationToken);

        var (canAdd, addError) = _mediaDomainService.ValidateAddMedia(
            existingMedias, request.ContentType);

        if (!canAdd)
            return ServiceResult<MediaDto>.Failure(addError!);

        
        string filePath;
        try
        {
            var directory = $"uploads/{request.EntityType.ToLowerInvariant()}/{request.EntityId}";
            filePath = await _storageService.UploadFileAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                directory,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در آپلود فایل {FileName}", request.FileName);
            return ServiceResult<MediaDto>.Failure("خطا در آپلود فایل.");
        }

        try
        {
            
            var media = Domain.Media.Media.Create(
                filePath,
                request.FileName,
                request.ContentType,
                request.FileSize,
                request.EntityType,
                request.EntityId,
                sortOrder: existingMedias.Count,
                isPrimary: request.IsPrimary || !existingMedias.Any(),
                altText: request.AltText);

            
            if (media.IsPrimary)
            {
                foreach (var existing in existingMedias.Where(m => m.IsPrimary))
                {
                    existing.RemovePrimary();
                    _mediaRepository.Update(existing);
                }
            }

            await _mediaRepository.AddAsync(media, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            
            var result = await _mediaQueryService.GetMediaByIdAsync(media.Id, cancellationToken);

            return ServiceResult<MediaDto>.Success(new MediaDto
            {
                Id = result!.Id,
                Url = result.Url,
                AltText = result.AltText,
                IsPrimary = result.IsPrimary,
                SortOrder = result.SortOrder
            });
        }
        catch (Exception ex)
        {
            
            _logger.LogError(ex, "خطا در ذخیره رسانه. حذف فایل آپلود شده.");
            try
            {
                await _storageService.DeleteFileAsync(filePath, cancellationToken);
            }
            catch (Exception deleteEx)
            {
                _logger.LogError(deleteEx, "خطا در حذف فایل بعد از Rollback: {FilePath}", filePath);
            }

            return ServiceResult<MediaDto>.Failure("خطا در ذخیره اطلاعات رسانه.");
        }
    }
}