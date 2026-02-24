namespace Application.Media.Contracts;

/// <summary>
/// سرویس ذخیره‌سازی فایل - پیاده‌سازی با S3/Liara در Infrastructure
/// فقط مسئول آپلود/حذف فیزیکی فایل‌ها
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// آپلود فایل و بازگرداندن مسیر ذخیره شده
    /// </summary>
    Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string directory,
        CancellationToken ct = default);

    /// <summary>
    /// حذف فیزیکی فایل
    /// </summary>
    Task DeleteFileAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت URL عمومی فایل
    /// </summary>
    string GetUrl(string filePath);

    /// <summary>
    /// لیست فایل‌های موجود در یک دایرکتوری (برای Cleanup)
    /// </summary>
    Task<IReadOnlyList<string>> GetFilesAsync(
        string directory,
        int maxResults,
        string? continuationToken,
        CancellationToken ct = default);

    /// <summary>
    /// بررسی وجود فایل
    /// </summary>
    Task<bool> FileExistsAsync(
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت فایل
    /// </summary>
    Task<Stream?> GetFileAsync(string filePath, CancellationToken ct = default);
}