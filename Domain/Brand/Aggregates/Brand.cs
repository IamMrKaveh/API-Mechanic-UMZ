using Domain.Brand.Events;
using Domain.Brand.Exceptions;
using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Brand.Aggregates;

public sealed class Brand : AggregateRoot<BrandId>, ISoftDeletable
{
    public BrandName Name { get; private set; } = null!;
    public BrandSlug Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? LogoPath { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    public CategoryId CategoryId { get; private set; } = default!;
    public Category.Aggregates.Category Category { get; private set; } = default!;

    private readonly List<ProductId> _products = [];
    public IReadOnlyCollection<ProductId> Products => _products;

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private Brand()
    { }

    private Brand(
        BrandId id,
        BrandName name,
        BrandSlug slug,
        CategoryId categoryId,
        string? description,
        string? logoPath) : base(id)
    {
        Name = name;
        Slug = slug;
        CategoryId = categoryId;
        Description = description;
        LogoPath = logoPath;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandCreatedEvent(id, name, slug, categoryId));
    }

    public static async Task<Brand> Create(
        BrandName name,
        BrandSlug slug,
        CategoryId categoryId,
        IBrandUniquenessChecker uniquenessChecker,
        string? description,
        string? logoPath,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(categoryId);
        ArgumentNullException.ThrowIfNull(uniquenessChecker);

        if (!await uniquenessChecker.IsUniqueAsync(name, slug, categoryId, null, ct))
            throw new BrandNameAlreadyExistsException(name);

        return new Brand(BrandId.NewId(), name, slug, categoryId, description, logoPath);
    }

    public async Task UpdateDetails(
        BrandName name,
        BrandSlug slug,
        IBrandUniquenessChecker uniquenessChecker,
        string? description,
        string? logoPath,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(uniquenessChecker);

        if (!await uniquenessChecker.IsUniqueAsync(name, slug, CategoryId, Id, ct))
            throw new BrandNameAlreadyExistsException(name);

        Name = name;
        Slug = slug;
        Description = description;
        LogoPath = logoPath;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        RaiseDomainEvent(new BrandUpdatedEvent(Id, name, slug, description));
    }

    public void ChangeCategory(CategoryId newCategoryId)
    {
        ArgumentNullException.ThrowIfNull(newCategoryId);

        if (CategoryId == newCategoryId)
            return;

        var previousCategoryId = CategoryId;
        CategoryId = newCategoryId;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        RaiseDomainEvent(new BrandCategoryChangedEvent(Id, previousCategoryId, newCategoryId));
    }

    public void Activate()
    {
        if (IsActive)
            throw new BrandAlreadyActiveException(Id);

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        RaiseDomainEvent(new BrandActivatedEvent(Id, Name, CategoryId));
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new BrandAlreadyDeactivatedException(Id);

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        RaiseDomainEvent(new BrandDeactivatedEvent(Id, Name, CategoryId));
    }
}