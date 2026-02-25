namespace Domain.Media.Services;

/// <summary>
/// Domain Service برای عملیات‌هایی که بین چند Media هستند
/// Stateless - بدون وابستگی به Infrastructure
/// </summary>
public class MediaDomainService
{
    private const int MaxMediaPerEntity = 20;

    /// <summary>
    /// اعتبارسنجی افزودن رسانه جدید به موجودیت
    /// </summary>
    public (bool CanAdd, string? Error) ValidateAddMedia(
        IEnumerable<Media> existingMedias,
        string fileType)
    {
        var mediaList = existingMedias.Where(m => !m.IsDeleted).ToList();

        if (mediaList.Count >= MaxMediaPerEntity)
        {
            return (false, $"حداکثر تعداد رسانه مجاز برای هر موجودیت {MaxMediaPerEntity} عدد است.");
        }

        return (true, null);
    }

    /// <summary>
    /// تنظیم رسانه اصلی و حذف اصلی بودن از سایر رسانه‌ها
    /// </summary>
    public void SetPrimaryMedia(Media newPrimary, IEnumerable<Media> allMedias)
    {
        Guard.Against.Null(newPrimary, nameof(newPrimary));

        if (!newPrimary.CanBeSetAsPrimary())
        {
            throw new DomainException("این رسانه قابل تنظیم به عنوان اصلی نیست.");
        }

        foreach (var media in allMedias.Where(m => m.IsPrimary && m.Id != newPrimary.Id))
        {
            media.RemovePrimary();
        }

        newPrimary.SetAsPrimary();
    }

    /// <summary>
    /// مرتب‌سازی مجدد رسانه‌ها
    /// </summary>
    public void ReorderMedias(IEnumerable<Media> medias, IReadOnlyList<int> orderedIds)
    {
        Guard.Against.Null(medias, nameof(medias));
        Guard.Against.Empty(orderedIds, nameof(orderedIds));

        var mediaDict = medias.Where(m => !m.IsDeleted).ToDictionary(m => m.Id);

        for (int i = 0; i < orderedIds.Count; i++)
        {
            if (mediaDict.TryGetValue(orderedIds[i], out var media))
            {
                media.UpdateSortOrder(i);
            }
        }
    }

    /// <summary>
    /// اعتبارسنجی نوع فایل برای موجودیت خاص
    /// </summary>
    public (bool IsValid, string? Error) ValidateFileTypeForEntity(
        string entityType,
        string fileExtension)
    {
        var imageExtensions = new[] { "jpg", "jpeg", "png", "gif", "webp", "bmp", "svg" };
        var documentExtensions = new[] { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt" };
        var videoExtensions = new[] { "mp4", "avi", "mkv", "mov", "wmv", "flv" };

        var extension = fileExtension.ToLowerInvariant().TrimStart('.');

        if (entityType.Equals("Product", StringComparison.OrdinalIgnoreCase) ||
            entityType.Equals("Brand", StringComparison.OrdinalIgnoreCase) ||
            entityType.Equals("Category", StringComparison.OrdinalIgnoreCase))
        {
            if (!imageExtensions.Contains(extension))
            {
                return (false, "برای این موجودیت فقط فایل‌های تصویری مجاز هستند.");
            }
        }

        var allAllowed = imageExtensions.Concat(documentExtensions).Concat(videoExtensions);
        if (!allAllowed.Contains(extension))
        {
            return (false, $"نوع فایل '{extension}' پشتیبانی نمی‌شود.");
        }

        return (true, null);
    }

    /// <summary>
    /// انتخاب رسانه اصلی پیش‌فرض پس از حذف اصلی فعلی
    /// </summary>
    public Media? SelectNewPrimaryAfterDeletion(IEnumerable<Media> remainingMedias)
    {
        return remainingMedias
            .Where(m => !m.IsDeleted && m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .FirstOrDefault();
    }
}