using Domain.Media.ValueObjects;

namespace Domain.Media.Services;

public static class MediaEntityTypes
{
    public const string Product = "Product";
    public const string Brand = "Brand";
    public const string Category = "Category";
}

public class MediaDomainService
{
    private const int MaxMediaPerEntity = 20;

    public static Result ValidateAddMedia(IEnumerable<Aggregates.Media> existingMedias, FilePath filePath)
    {
        var mediaList = existingMedias.Where(m => m.IsActive).ToList();

        if (mediaList.Count >= MaxMediaPerEntity)
            return Result.Failure(new Error(
                "Media.LimitExceeded",
                $"حداکثر تعداد رسانه مجاز برای هر موجودیت {MaxMediaPerEntity} عدد است.",
                ErrorType.Validation));

        return Result.Success();
    }

    public static Result ValidateFileTypeForEntity(string entityType, FilePath filePath)
    {
        if (entityType.Equals(MediaEntityTypes.Product, StringComparison.OrdinalIgnoreCase) ||
            entityType.Equals(MediaEntityTypes.Brand, StringComparison.OrdinalIgnoreCase) ||
            entityType.Equals(MediaEntityTypes.Category, StringComparison.OrdinalIgnoreCase))
        {
            if (!filePath.IsImage())
                return Result.Failure(new Error(
                    "Media.InvalidType",
                    "برای این موجودیت فقط فایل‌های تصویری مجاز هستند.",
                    ErrorType.Validation));
        }

        if (!filePath.IsImage() && !filePath.IsDocument() && !filePath.IsVideo())
            return Result.Failure(new Error(
                "Media.UnsupportedType",
                $"نوع فایل '{filePath.Extension}' پشتیبانی نمی‌شود.",
                ErrorType.Validation));

        return Result.Success();
    }

    public static void SetPrimaryMedia(Aggregates.Media newPrimary, IEnumerable<Aggregates.Media> allMedias)
    {
        Guard.Against.Null(newPrimary, nameof(newPrimary));

        if (!newPrimary.CanBeSetAsPrimary())
            throw new DomainException("این رسانه قابل تنظیم به عنوان اصلی نیست.");

        foreach (var media in allMedias.Where(m => m.IsPrimary && m.Id != newPrimary.Id))
        {
            media.RemovePrimary();
        }

        newPrimary.SetAsPrimary();
    }

    public static void ReorderMedias(IEnumerable<Aggregates.Media> medias, IReadOnlyList<MediaId> orderedIds)
    {
        Guard.Against.Null(medias, nameof(medias));
        Guard.Against.Empty(orderedIds, nameof(orderedIds));

        var activeMedias = medias.Where(m => m.IsActive).ToList();
        var activeMediaDict = activeMedias.ToDictionary(m => m.Id);

        var activeIdSet = new HashSet<MediaId>(activeMediaDict.Keys);
        var providedIdSet = new HashSet<MediaId>(orderedIds);

        var invalidIds = providedIdSet.Except(activeIdSet).ToList();
        if (invalidIds.Any())
            throw new DomainException($"شناسه‌های رسانه نامعتبر یا غیرفعال: {string.Join(", ", invalidIds.Select(id => id.Value))}");

        var missingIds = activeIdSet.Except(providedIdSet).ToList();
        if (missingIds.Any())
            throw new DomainException($"تمام رسانه‌های فعال باید در لیست مرتب‌سازی وجود داشته باشند. شناسه‌های ناموجود: {string.Join(", ", missingIds.Select(id => id.Value))}");

        var distinctCount = orderedIds.Distinct().Count();
        if (distinctCount != orderedIds.Count)
            throw new DomainException("لیست مرتب‌سازی شامل شناسه‌های تکراری است.");

        for (int i = 0; i < orderedIds.Count; i++)
        {
            activeMediaDict[orderedIds[i]].UpdateSortOrder(i);
        }
    }

    public static Aggregates.Media? SelectNewPrimaryAfterDeletion(IEnumerable<Aggregates.Media> remainingMedias)
    {
        return remainingMedias
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .FirstOrDefault();
    }
}