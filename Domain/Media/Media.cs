namespace Domain.Media;

public class Media : AggregateRoot, IAuditable, ISoftDeletable, IActivatable
{
    private readonly List<string> _allowedImageExtensions = new() { "jpg", "jpeg", "png", "gif", "webp", "bmp", "svg" };
    private readonly List<string> _allowedDocumentExtensions = new() { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt" };
    private readonly List<string> _allowedVideoExtensions = new() { "mp4", "avi", "mkv", "mov", "wmv", "flv" };

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB
    private const int MaxFileNameLength = 255;
    private const int MaxAltTextLength = 500;

    public string FilePath { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public string FileType { get; private set; } = null!;
    public long FileSize { get; private set; }
    public string EntityType { get; private set; } = null!;
    public int EntityId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public string? AltText { get; private set; }

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Activation
    public bool IsActive { get; private set; }

    // Computed Properties
    public string Extension => GetExtension();

    public bool IsImage => _allowedImageExtensions.Contains(Extension);
    public bool IsDocument => _allowedDocumentExtensions.Contains(Extension);
    public bool IsVideo => _allowedVideoExtensions.Contains(Extension);
    public string FileSizeDisplay => FormatFileSize(FileSize);

    private Media()
    { }

    #region Factory Methods

    public static Media Create(
        string filePath,
        string fileName,
        string fileType,
        long fileSize,
        string entityType,
        int entityId,
        int sortOrder = 0,
        bool isPrimary = false,
        string? altText = null)
    {
        Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));
        Guard.Against.NullOrWhiteSpace(fileName, nameof(fileName));
        Guard.Against.NullOrWhiteSpace(fileType, nameof(fileType));
        Guard.Against.NullOrWhiteSpace(entityType, nameof(entityType));
        Guard.Against.NegativeOrZero(entityId, nameof(entityId));

        ValidateFileName(fileName);
        ValidateFileSize(fileSize);
        ValidateFileType(fileType);
        ValidateSortOrder(sortOrder);
        ValidateAltText(altText);

        var media = new Media
        {
            FilePath = NormalizePath(filePath),
            FileName = fileName.Trim(),
            FileType = fileType.Trim().ToLowerInvariant(),
            FileSize = fileSize,
            EntityType = entityType.Trim(),
            EntityId = entityId,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            AltText = altText?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        media.AddDomainEvent(new MediaCreatedEvent(media.Id, media.EntityType, media.EntityId));

        return media;
    }

    public static Media CreateImage(
        string filePath,
        string fileName,
        long fileSize,
        string entityType,
        int entityId,
        int sortOrder = 0,
        bool isPrimary = false,
        string? altText = null)
    {
        var extension = GetExtensionFromFileName(fileName);
        var allowedExtensions = new[] { "jpg", "jpeg", "png", "gif", "webp", "bmp", "svg" };

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidFileTypeException(extension, allowedExtensions);
        }

        var fileType = GetContentTypeFromExtension(extension);

        return Create(filePath, fileName, fileType, fileSize, entityType, entityId, sortOrder, isPrimary, altText);
    }

    #endregion Factory Methods

    #region Update Methods

    public void UpdateMetadata(string? altText, int sortOrder)
    {
        EnsureNotDeleted();

        ValidateAltText(altText);
        ValidateSortOrder(sortOrder);

        AltText = altText?.Trim();
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAltText(string? altText)
    {
        EnsureNotDeleted();
        ValidateAltText(altText);

        AltText = altText?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSortOrder(int sortOrder)
    {
        EnsureNotDeleted();
        ValidateSortOrder(sortOrder);

        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsPrimary()
    {
        EnsureNotDeleted();
        EnsureActive();

        if (IsPrimary) return;

        IsPrimary = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new MediaSetAsPrimaryEvent(Id, EntityType, EntityId));
    }

    public void RemovePrimary()
    {
        EnsureNotDeleted();

        if (!IsPrimary) return;

        IsPrimary = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MoveToEntity(string entityType, int entityId)
    {
        EnsureNotDeleted();
        Guard.Against.NullOrWhiteSpace(entityType, nameof(entityType));
        Guard.Against.NegativeOrZero(entityId, nameof(entityId));

        var oldEntityType = EntityType;
        var oldEntityId = EntityId;

        EntityType = entityType.Trim();
        EntityId = entityId;
        IsPrimary = false; // Reset primary when moving
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new MediaMovedEvent(Id, oldEntityType, oldEntityId, EntityType, EntityId));
    }

    #endregion Update Methods

    #region Activation & Deletion

    public void Activate()
    {
        if (IsActive) return;
        EnsureNotDeleted();

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        IsPrimary = false; // Cannot be primary if inactive
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
        IsPrimary = false;

        AddDomainEvent(new MediaDeletedEvent(Id, EntityType, EntityId, deletedBy));
    }

    public void Restore()
    {
        if (!IsDeleted) return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Activation & Deletion

    #region Query Methods

    public string GetContentType()
    {
        return GetContentTypeFromExtension(Extension);
    }

    public bool CanBeSetAsPrimary()
    {
        return !IsDeleted && IsActive && !IsPrimary;
    }

    public bool BelongsTo(string entityType, int entityId)
    {
        return EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase) &&
               EntityId == entityId;
    }

    #endregion Query Methods

    #region Private Methods

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("رسانه حذف شده است.");
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("رسانه غیرفعال است.");
    }

    private string GetExtension()
    {
        return GetExtensionFromFileName(FileName);
    }

    private static string GetExtensionFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var lastDot = fileName.LastIndexOf('.');
        if (lastDot < 0 || lastDot == fileName.Length - 1)
            return string.Empty;

        return fileName.Substring(lastDot + 1).ToLowerInvariant();
    }

    private static string NormalizePath(string path)
    {
        return path.Trim()
            .Replace('\\', '/')
            .TrimStart('/');
    }

    private static void ValidateFileName(string fileName)
    {
        if (fileName.Length > MaxFileNameLength)
            throw new DomainException($"نام فایل نمی‌تواند بیش از {MaxFileNameLength} کاراکتر باشد.");

        var invalidChars = new[] { "..", ":", "*", "?", "\"", "<", ">", "|" };
        foreach (var invalid in invalidChars)
        {
            if (fileName.Contains(invalid))
                throw new DomainException($"نام فایل شامل کاراکتر غیرمجاز '{invalid}' است.");
        }
    }

    private static void ValidateFileSize(long fileSize)
    {
        if (fileSize <= 0)
            throw new DomainException("حجم فایل باید بزرگتر از صفر باشد.");

        if (fileSize > MaxFileSizeBytes)
            throw new DomainException($"حجم فایل نمی‌تواند بیش از {MaxFileSizeBytes / (1024 * 1024)} مگابایت باشد.");
    }

    private static void ValidateFileType(string fileType)
    {
        if (string.IsNullOrWhiteSpace(fileType))
            throw new DomainException("نوع فایل الزامی است.");
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
            throw new DomainException("ترتیب نمایش نمی‌تواند منفی باشد.");
    }

    private static void ValidateAltText(string? altText)
    {
        if (!string.IsNullOrWhiteSpace(altText) && altText.Length > MaxAltTextLength)
            throw new DomainException($"متن جایگزین نمی‌تواند بیش از {MaxAltTextLength} کاراکتر باشد.");
    }

    private static string GetContentTypeFromExtension(string extension)
    {
        return extension switch
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

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "بایت", "کیلوبایت", "مگابایت", "گیگابایت" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    #endregion Private Methods
}