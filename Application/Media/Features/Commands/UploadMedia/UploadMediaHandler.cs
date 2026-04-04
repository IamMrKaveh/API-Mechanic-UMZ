using Application.Common.Results;
using Application.Media.Contracts;
using Application.Media.Features.Shared;
using Domain.Common.Interfaces;
using Domain.Media.Interfaces;
using Domain.Media.Services;

namespace Application.Media.Features.Commands.UploadMedia;

public class UploadMediaHandler(
    IMediaRepository mediaRepository,
    IStorageService storageService,
    IMediaQueryService mediaQueryService,
    MediaDomainService mediaDomainService,
    IUnitOfWork unitOfWork,
    ILogger<UploadMediaHandler> logger) : IRequestHandler<UploadMediaCommand, ServiceResult<MediaDto>>
{
    private readonly IMediaRepository _mediaRepository = mediaRepository;
    private readonly IStorageService _storageService = storageService;
    private readonly IMediaQueryService _mediaQueryService = mediaQueryService;
    private readonly MediaDomainService _mediaDomainService = mediaDomainService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<UploadMediaHandler> _logger = logger;

    public async Task<ServiceResult<MediaDto>> Handle(
        UploadMediaCommand request,
        CancellationToken ct)
    {
        var extension = Path.GetExtension(request.FileName).TrimStart('.');
        var (isValidType, typeError) = _mediaDomainService.ValidateFileTypeForEntity(
            request.EntityType, extension);

        if (!isValidType)
            return ServiceResult<MediaDto>.Validation(typeError!);

        var existingMedias = await _mediaRepository.GetByEntityAsync(
            request.EntityType, request.EntityId, ct);

        var (canAdd, addError) = _mediaDomainService.ValidateAddMedia(
            existingMedias, request.ContentType);

        if (!canAdd)
            return ServiceResult<MediaDto>.Forbidden(addError!);

        string filePath;
        try
        {
            var directory = $"uploads/{request.EntityType.ToLowerInvariant()}/{request.EntityId}";
            filePath = await _storageService.UploadFileAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                directory,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در آپلود فایل {FileName}", request.FileName);
            return ServiceResult<MediaDto>.Unexpected("خطا در آپلود فایل.");
        }

        try
        {
            var media = Domain.Media.Aggregates.Media.Create(
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

            await _mediaRepository.AddAsync(media, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var result = await _mediaQueryService.GetMediaByIdAsync(media.Id, ct);

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
                await _storageService.DeleteFileAsync(filePath, ct);
            }
            catch (Exception deleteEx)
            {
                _logger.LogError(deleteEx, "خطا در حذف فایل بعد از Rollback: {FilePath}", filePath);
            }

            return ServiceResult<MediaDto>.Unexpected("خطا در ذخیره اطلاعات رسانه.");
        }
    }
}