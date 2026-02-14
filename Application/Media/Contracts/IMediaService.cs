namespace Application.Media.Contracts;

/// <summary>
/// سرویس هماهنگی رسانه - Facade برای استفاده سایر فیچرها
/// بدون Business Logic - فقط هماهنگی بین Repository، Storage و Domain
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// آپلود و اتصال فایل به موجودیت
    /// </summary>
    Task<Domain.Media.Media> AttachFileToEntityAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string entityType,
        int entityId,
        bool isPrimary = false,
        string? altText = null,
        bool saveChanges = true,
        CancellationToken ct = default);

    /// <summary>
    /// حذف نرم رسانه
    /// </summary>
    Task DeleteMediaAsync(int mediaId, int? deletedBy = null, CancellationToken ct = default);

    /// <summary>
    /// دریافت رسانه‌های یک موجودیت (DTO)
    /// </summary>
    Task<IReadOnlyList<MediaDto>> GetEntityMediaAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت URL تصویر اصلی
    /// </summary>
    Task<string?> GetPrimaryImageUrlAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);
}