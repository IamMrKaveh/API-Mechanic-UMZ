using Slug = Domain.Brand.ValueObjects.Slug;

namespace Domain.Brand.Aggregates;

public sealed class Brand : AggregateRoot<Guid>
{
    public BrandName Name { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? LogoPath { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    private Brand()
    { }

    private Brand(
        Guid id,
        BrandName name,
        Slug slug,
        Guid categoryId,
        string? description,
        string? logoPath) : base(id)
    {
        Name = name;
        Slug = slug;
        CategoryId = categoryId;
        Description = description;
        LogoPath = logoPath;
        IsActive = true;
        IsDeleted = false;
        CreatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandCreatedEvent(id, name.Value, slug.Value, categoryId));
    }

    public static Brand Create(
        BrandName name,
        Slug slug,
        Guid categoryId,
        string? description = null,
        string? logoPath = null)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));

        return new Brand(Guid.NewGuid(), name, slug, categoryId, description, logoPath);
    }

    public void UpdateDetails(BrandName name, Slug slug, string? description, string? logoPath)
    {
        EnsureNotDeleted();

        Name = name;
        Slug = slug;
        Description = description;
        LogoPath = logoPath;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandUpdatedEvent(Id, name.Value, slug.Value, description));
    }

    public void ChangeCategory(Guid newCategoryId)
    {
        EnsureNotDeleted();

        if (newCategoryId == Guid.Empty)
            throw new ArgumentException("Category ID cannot be empty.", nameof(newCategoryId));

        if (CategoryId == newCategoryId)
            return;

        var previousCategoryId = CategoryId;
        CategoryId = newCategoryId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandCategoryChangedEvent(Id, previousCategoryId, newCategoryId));
    }

    public void Activate()
    {
        EnsureNotDeleted();

        if (IsActive)
            throw new BrandAlreadyActiveException(Id);

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandActivatedEvent(Id, Name.Value, CategoryId));
    }

    public void Deactivate()
    {
        EnsureNotDeleted();

        if (!IsActive)
            throw new BrandAlreadyDeactivatedException(Id);

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandDeactivatedEvent(Id, Name.Value, CategoryId));
    }

    public void Delete()
    {
        EnsureNotDeleted();
        IsDeleted = true;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new BrandDeletedEvent(Id, CategoryId));
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DeletedBrandMutationException(Id);
    }
}