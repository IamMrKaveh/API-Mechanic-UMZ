using Domain.Common.Exceptions;

namespace Domain.Media.Exceptions;

public sealed class InvalidFileTypeException : DomainException
{
    public string FileType { get; }
    public IReadOnlyList<string> AllowedTypes { get; }

    public override string ErrorCode => "INVALID_FILE_TYPE";

    public InvalidFileTypeException(string fileType, IEnumerable<string> allowedTypes)
        : base($"نوع فایل '{fileType}' مجاز نیست. انواع مجاز: {string.Join(", ", allowedTypes)}")
    {
        FileType = fileType;
        AllowedTypes = allowedTypes.ToList().AsReadOnly();
    }

    public InvalidFileTypeException(string fileType)
        : base($"نوع فایل '{fileType}' مجاز نیست.")
    {
        FileType = fileType;
        AllowedTypes = Array.Empty<string>();
    }
}