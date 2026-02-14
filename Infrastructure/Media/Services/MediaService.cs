using Application.Common.Contracts;

namespace Infrastructure.Media.Services;

/// <summary>
/// پیاده‌سازی IMediaService - Facade برای استفاده سایر فیچرها
/// هماهنگی بین Repository، Storage و Domain Service
/// </summary>
public class MediaService : IMediaService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IStorageService _storageService;
    private readonly IMediaQueryService _mediaQueryService;
    private readonly MediaDomainService _mediaDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        IMediaQueryService mediaQueryService,
        MediaDomainService mediaDomainService,
        IUnitOfWork unitOfWork,
        ILogger<MediaService> logger)
    {
        _mediaRepository = mediaRepository;
        _storageService = storageService;
        _mediaQueryService = mediaQueryService;
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
        // 1. اعتبارسنجی نوع فایل
        var extension = Path.GetExtension(fileName).TrimStart('.');
        var (isValidType, typeError) = _mediaDomainService.ValidateFileTypeForEntity(entityType, extension);
        if (!isValidType)
            throw new DomainException(typeError!);

        // 2. بررسی محدودیت تعداد
        var existingMedias = await _mediaRepository.GetByEntityAsync(entityType, entityId, ct);
        var (canAdd, addError) = _mediaDomainService.ValidateAddMedia(existingMedias, contentType);
        if (!canAdd)
            throw new DomainException(addError!);

        // 3. آپلود فیزیکی
        var directory = $"uploads/{entityType.ToLowerInvariant()}/{entityId}";
        var filePath = await _storageService.UploadFileAsync(
            fileStream, fileName, contentType, directory, ct);

        try
        {
            // 4. ایجاد Aggregate
            var shouldBePrimary = isPrimary || !existingMedias.Any();
            var media = Domain.Media.Media.Create(
                filePath, fileName, contentType, fileSize,
                entityType, entityId,
                sortOrder: existingMedias.Count,
                isPrimary: shouldBePrimary,
                altText: altText);

            // 5. مدیریت Primary
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

        // حذف نرم از طریق Aggregate
        media.Delete(deletedBy);
        _mediaRepository.Update(media);

        // انتخاب Primary جدید
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

    public async Task<IReadOnlyList<MediaDto>> GetEntityMediaAsync(
        string entityType, int entityId, CancellationToken ct = default)
    {
        return await _mediaQueryService.GetEntityMediaAsync(entityType, entityId, ct);
    }

    public async Task<string?> GetPrimaryImageUrlAsync(
        string entityType, int entityId, CancellationToken ct = default)
    {
        return await _mediaQueryService.GetPrimaryImageUrlAsync(entityType, entityId, ct);
    }
}