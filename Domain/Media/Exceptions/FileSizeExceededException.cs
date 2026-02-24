namespace Domain.Media.Exceptions;

public class FileSizeExceededException : DomainException
{
    public long FileSize { get; }
    public long MaxAllowedSize { get; }

    public FileSizeExceededException(long fileSize, long maxAllowedSize)
        : base($"حجم فایل ({fileSize / (1024 * 1024)} مگابایت) بیشتر از حد مجاز ({maxAllowedSize / (1024 * 1024)} مگابایت) است.")
    {
        FileSize = fileSize;
        MaxAllowedSize = maxAllowedSize;
    }
}