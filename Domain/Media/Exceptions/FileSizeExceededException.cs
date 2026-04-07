using Domain.Common.Exceptions;

namespace Domain.Media.Exceptions;

public sealed class FileSizeExceededException : DomainException
{
    public long FileSize { get; }
    public long MaxAllowedSize { get; }

    public override string ErrorCode => "FILE_SIZE_EXCEEDED";

    public FileSizeExceededException(long fileSize, long maxAllowedSize)
        : base($"حجم فایل ({fileSize / (1024 * 1024)} مگابایت) بیشتر از حد مجاز ({maxAllowedSize / (1024 * 1024)} مگابایت) است.")
    {
        FileSize = fileSize;
        MaxAllowedSize = maxAllowedSize;
    }
}