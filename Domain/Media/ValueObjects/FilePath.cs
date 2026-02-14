namespace Domain.Media.ValueObjects;

public sealed class FilePath : ValueObject
{
    public string Value { get; }
    public string FileName { get; }
    public string Extension { get; }
    public string Directory { get; }

    private const int MaxPathLength = 500;

    private FilePath(string value, string fileName, string extension, string directory)
    {
        Value = value;
        FileName = fileName;
        Extension = extension;
        Directory = directory;
    }

    public static FilePath Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new DomainException("مسیر فایل الزامی است.");

        var normalized = NormalizePath(path);
        Validate(normalized);

        var fileName = GetFileName(normalized);
        var extension = GetExtension(fileName);
        var directory = GetDirectory(normalized);

        return new FilePath(normalized, fileName, extension, directory);
    }

    public static FilePath CreateForUpload(string directory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new DomainException("دایرکتوری الزامی است.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("نام فایل الزامی است.");

        var path = CombinePath(directory, fileName);
        return Create(path);
    }

    public bool IsImage()
    {
        var imageExtensions = new[] { "jpg", "jpeg", "png", "gif", "webp", "bmp", "svg" };
        return imageExtensions.Contains(Extension);
    }

    public bool IsDocument()
    {
        var docExtensions = new[] { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt" };
        return docExtensions.Contains(Extension);
    }

    public bool IsVideo()
    {
        var videoExtensions = new[] { "mp4", "avi", "mkv", "mov", "wmv", "flv" };
        return videoExtensions.Contains(Extension);
    }

    public string GetContentType()
    {
        return Extension switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "webp" => "image/webp",
            "svg" => "image/svg+xml",
            "bmp" => "image/bmp",
            "pdf" => "application/pdf",
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xls" => "application/vnd.ms-excel",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ppt" => "application/vnd.ms-powerpoint",
            "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "txt" => "text/plain",
            "mp4" => "video/mp4",
            "avi" => "video/x-msvideo",
            "mkv" => "video/x-matroska",
            "mov" => "video/quicktime",
            "wmv" => "video/x-ms-wmv",
            "flv" => "video/x-flv",
            _ => "application/octet-stream"
        };
    }

    public FilePath WithNewFileName(string newFileName)
    {
        if (string.IsNullOrWhiteSpace(newFileName))
            throw new DomainException("نام فایل جدید الزامی است.");

        return CreateForUpload(Directory, newFileName);
    }

    private static string NormalizePath(string path)
    {
        return path.Trim()
            .Replace('\\', '/')
            .TrimStart('/');
    }

    private static void Validate(string path)
    {
        var invalidChars = new[] { "..", ":", "*", "?", "\"", "<", ">", "|" };
        foreach (var invalid in invalidChars)
        {
            if (path.Contains(invalid))
                throw new DomainException($"مسیر فایل شامل کاراکتر غیرمجاز '{invalid}' است.");
        }

        if (path.Length > MaxPathLength)
            throw new DomainException($"مسیر فایل نمی‌تواند بیش از {MaxPathLength} کاراکتر باشد.");
    }

    private static string GetFileName(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
    }

    private static string GetExtension(string fileName)
    {
        var lastDot = fileName.LastIndexOf('.');
        if (lastDot < 0 || lastDot == fileName.Length - 1)
            return string.Empty;

        return fileName.Substring(lastDot + 1).ToLowerInvariant();
    }

    private static string GetDirectory(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        return lastSlash >= 0 ? path.Substring(0, lastSlash) : string.Empty;
    }

    private static string CombinePath(string directory, string fileName)
    {
        var normalizedDir = directory.TrimEnd('/');
        var normalizedFile = fileName.TrimStart('/');
        return $"{normalizedDir}/{normalizedFile}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(FilePath path) => path.Value;
}