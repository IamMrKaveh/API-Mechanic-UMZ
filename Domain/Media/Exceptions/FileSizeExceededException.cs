namespace Domain.Media.Exceptions;

public class FileSizeExceededException(long fileSize, long maxAllowedSize) : DomainException($"حجم فایل ({fileSize / (1024 * 1024)} مگابایت) بیشتر از حد مجاز ({maxAllowedSize / (1024 * 1024)} مگابایت) است.")
{
    public long FileSize { get; } = fileSize;
    public long MaxAllowedSize { get; } = maxAllowedSize;
}