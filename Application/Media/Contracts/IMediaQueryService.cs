namespace Application.Media.Contracts;

/// <summary>
/// سرویس کوئری رسانه‌ها - مستقیماً DTO برمی‌گرداند.
/// بدون بارگذاری Aggregate - بهینه برای خواندن.
/// </summary>
public interface IMediaQueryService
{
    /// <summary>
    /// دریافت رسانه‌های یک موجودیت
    /// </summary>
    Task<IReadOnlyList<MediaDto>> GetEntityMediaAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت URL تصویر اصلی یک موجودیت
    /// </summary>
    Task<string?> GetPrimaryImageUrlAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت جزئیات یک رسانه
    /// </summary>
    Task<MediaDetailDto?> GetMediaByIdAsync(
        int mediaId,
        CancellationToken ct = default);

    /// <summary>
    /// لیست صفحه‌بندی شده تمام رسانه‌ها (Admin)
    /// </summary>
    Task<PaginatedResult<MediaListItemDto>> GetAllMediaPagedAsync(
        string? entityType,
        int page,
        int pageSize,
        CancellationToken ct = default);
}