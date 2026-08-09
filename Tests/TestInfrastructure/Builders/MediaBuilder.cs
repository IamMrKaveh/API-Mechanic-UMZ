using Domain.Media.Aggregates;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class MediaBuilder
{
    private static readonly Faker Faker = new();

    private string _filePath = $"uploads/{Faker.Random.AlphaNumeric(6).ToLowerInvariant()}/{Faker.System.FileName("png")}";
    private string _fileName = Faker.System.FileName("png");
    private string _fileType = "image/png";
    private long _fileSize = Faker.Random.Long(1, 5 * 1024 * 1024);
    private string _entityType = "Product";
    private Guid _entityId = Guid.NewGuid();
    private int _sortOrder = 0;
    private bool _isPrimary = false;
    private string? _altText = null;

    public MediaBuilder WithFilePath(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    public MediaBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public MediaBuilder WithFileType(string fileType)
    {
        _fileType = fileType;
        return this;
    }

    public MediaBuilder WithFileSize(long fileSize)
    {
        _fileSize = fileSize;
        return this;
    }

    public MediaBuilder WithEntityType(string entityType)
    {
        _entityType = entityType;
        return this;
    }

    public MediaBuilder WithEntityId(Guid entityId)
    {
        _entityId = entityId;
        return this;
    }

    public MediaBuilder WithSortOrder(int sortOrder)
    {
        _sortOrder = sortOrder;
        return this;
    }

    public MediaBuilder WithIsPrimary(bool isPrimary)
    {
        _isPrimary = isPrimary;
        return this;
    }

    public MediaBuilder WithAltText(string? altText)
    {
        _altText = altText;
        return this;
    }

    public Media Build() =>
        Media.Create(
            _filePath,
            _fileName,
            _fileType,
            _fileSize,
            _entityType,
            _entityId,
            _sortOrder,
            _isPrimary,
            _altText);

    public Media BuildDeleted(UserId? deletedBy = null)
    {
        var media = Build();
        media.RequestDeletion(deletedBy);
        return media;
    }

    public Media BuildPrimary()
    {
        var media = WithIsPrimary(false).Build();
        media.SetAsPrimary();
        return media;
    }
}

