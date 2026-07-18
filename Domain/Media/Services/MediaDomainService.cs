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

    public static ServiceResult ValidateFileTypeForEntity(string entityType, FilePath filePath)
    {
        if (entityType.Equals(MediaEntityTypes.Product, StringComparison.OrdinalIgnoreCase) ||
            entityType.Equals(MediaEntityTypes.Brand, StringComparison.OrdinalIgnoreCase) ||
            entityType.Equals(MediaEntityTypes.Category, StringComparison.OrdinalIgnoreCase))
        {
            if (!filePath.IsImage())
                return ServiceResult.Failure(new Error(
                    "Media.InvalidType",
                    "برای این موجودیت فقط فایل‌های تصویری مجاز هستند.",
                    ErrorType.Validation));
        }

        if (!filePath.IsImage() && !filePath.IsDocument() && !filePath.IsVideo())
            return ServiceResult.Failure(new Error(
                "Media.UnsupportedType",
                $"نوع فایل '{filePath.Extension}' پشتیبانی نمی‌شود.",
                ErrorType.Validation));

        return ServiceResult.Success();
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