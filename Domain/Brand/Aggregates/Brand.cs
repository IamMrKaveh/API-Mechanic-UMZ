using Domain.Brand.Events;
using Domain.Brand.Exceptions;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Aggregates;

public sealed class Brand : AggregateRoot<BrandId>
{
    public BrandName Name { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? LogoPath { get; private set; }
    public CategoryId CategoryId { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    private Brand()
    { }

    private Brand(
        BrandId id,
        BrandName name,
        Slug slug,
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

        RaiseDomainEvent(new BrandCreatedEvent(id.Value, name.Value, slug.Value, categoryId.Value));
    }

    public static Brand Create(
        BrandName name,
        Slug slug,
        CategoryId categoryId,
        string? description = null,
        string? logoPath = null)
    {
        ArgumentNullException.ThrowIfNull(categoryId);
        return new Brand(BrandId.NewId(), name, slug, categoryId, description, logoPath);
    }

    public void UpdateDetails(BrandName name, Slug slug, string? description, string? logoPath)
    {
        Name = name;
        Slug = slug;
        Description = description;
        LogoPath = logoPath;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandUpdatedEvent(Id.Value, name.Value, slug.Value, description));
    }

    public void ChangeCategory(CategoryId newCategoryId)
    {
        ArgumentNullException.ThrowIfNull(newCategoryId);

        if (CategoryId == newCategoryId)
            return;

        var previousCategoryId = CategoryId;
        CategoryId = newCategoryId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandCategoryChangedEvent(Id.Value, previousCategoryId.Value, newCategoryId.Value));
    }

    public void Activate()
    {
        if (IsActive)
            throw new BrandAlreadyActiveException(Id.Value);

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandActivatedEvent(Id.Value, Name.Value, CategoryId.Value));
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new BrandAlreadyDeactivatedException(Id.Value);

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandDeactivatedEvent(Id.Value, Name.Value, CategoryId.Value));
    }
}