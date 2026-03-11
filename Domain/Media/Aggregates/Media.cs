namespace Domain.Media.Aggregates;

public class Media : AggregateRoot<MediaId>, IAuditable, IActivatable
{
    private const int MaxAltTextLength = 500;

    public FilePath Path { get; private set; } = null!;
    public FileSize Size { get; private set; } = null!;
    public string FileType { get; private set; } = null!;
    public string EntityType { get; private set; } = null!;
    public int EntityId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public string? AltText { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public string FilePath => Path.Value;
    public string FileName => Path.FileName;
    public string Extension => Path.Extension;
    public long FileSize => Size.Bytes;

    private Media()
    { }

    private Media(
        MediaId id,
        ValueObjects.FilePath path,
        ValueObjects.FileSize size,
        string fileType,
        string entityType,
        int entityId,
        int sortOrder,
        bool isPrimary,
        string? altText) : base(id)
    {
        Path = path;
        Size = size;
        FileType = fileType;
        EntityType = entityType;
        EntityId = entityId;
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
        AltText = altText;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new MediaCreatedEvent(id.Value, entityType, entityId));
    }

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
        Guard.Against.NullOrWhiteSpace(fileType, nameof(fileType));
        Guard.Against.NullOrWhiteSpace(entityType, nameof(entityType));
        Guard.Against.NegativeOrZero(entityId, nameof(entityId));

        ValidateFileType(fileType);
        ValidateSortOrder(sortOrder);
        ValidateAltText(altText);

        var normalizedFileType = fileType.Trim().ToLowerInvariant();

        var path = ValueObjects.FilePath.CreateForUpload(
            GetDirectoryFromPath(filePath),
            fileName);
        var size = ValueObjects.FileSize.Create(fileSize);

        return new Media(
            MediaId.NewId(),
            path,
            size,
            normalizedFileType,
            entityType.Trim(),
            entityId,
            sortOrder,
            isPrimary,
            altText?.Trim());
    }

    public void UpdateSortOrder(int sortOrder)
    {
        ValidateSortOrder(sortOrder);
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsPrimary()
    {
        EnsureActive();

        if (IsPrimary) return;

        IsPrimary = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new MediaSetAsPrimaryEvent(Id.Value, EntityType, EntityId));
    }

    public void RemovePrimary()
    {
        if (!IsPrimary) return;

        IsPrimary = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RequestDeletion(int? deletedBy = null)
    {
        IsActive = false;
        IsPrimary = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new MediaDeletedEvent(Id.Value, EntityType, EntityId, deletedBy));
    }

    public bool CanBeSetAsPrimary() => IsActive && !IsPrimary;

    public string GetContentType() => Path.GetContentType();

    public bool IsImage() => Path.IsImage();

    public bool IsDocument() => Path.IsDocument();

    public bool IsVideo() => Path.IsVideo();

    public string GetDisplaySize() => Size.ToDisplayString();

    public void UpdateAltText(string? altText)
    {
        ValidateAltText(altText);
        AltText = altText?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("رسانه غیرفعال است.");
    }

    private static string GetDirectoryFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var normalized = path.Trim().Replace('\\', '/').TrimStart('/');
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash >= 0 ? normalized.Substring(0, lastSlash) : string.Empty;
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
}