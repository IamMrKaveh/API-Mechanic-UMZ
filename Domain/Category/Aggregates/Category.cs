using Domain.Category.Events;
using Domain.Category.Exceptions;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Domain.Category.Aggregates;

public sealed class Category : AggregateRoot<CategoryId>
{
    private Category()
    { }

    public string Name { get; private set; } = default!;
    public Slug Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public CategoryId? ParentCategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public bool IsRootCategory => ParentCategoryId is null;

    public static Category Create(
        CategoryId id,
        CategoryName name,
        Slug slug,
        ICategoryUniquenessChecker uniquenessChecker,
        string? description = null,
        CategoryId? parentCategoryId = null,
        int sortOrder = 0)
    {
        ArgumentNullException.ThrowIfNull(uniquenessChecker);

        if (!uniquenessChecker.IsUnique(name, slug.Value))
            throw new DuplicateCategoryNameException(name);

        if (parentCategoryId != null && parentCategoryId == id)
            throw new DomainException("دسته‌بندی نمی‌تواند والد خودش باشد.");

        var category = new Category
        {
            Id = id,
            Name = name,
            Slug = slug,
            Description = description,
            ParentCategoryId = parentCategoryId,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        category.RaiseDomainEvent(new CategoryCreatedEvent(id, name, slug, parentCategoryId));
        return category;
    }

    public void UpdateDetails(
        CategoryName name,
        Slug slug,
        ICategoryUniquenessChecker uniquenessChecker,
        string? description,
        int sortOrder)
    {
        ArgumentNullException.ThrowIfNull(uniquenessChecker);

        if (!uniquenessChecker.IsUnique(name, slug.Value, Id))
            throw new DuplicateCategoryNameException(name);

        Name = name;
        Slug = slug;
        Description = description;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        RaiseDomainEvent(new CategoryUpdatedEvent(Id, name, slug, description));
    }

    public void MoveToParent(CategoryId? newParentCategoryId)
    {
        if (newParentCategoryId == Id)
            throw new DomainException("دسته‌بندی نمی‌تواند والد خودش باشد.");

        if (ParentCategoryId == newParentCategoryId)
            return;

        var previousParent = ParentCategoryId;
        ParentCategoryId = newParentCategoryId;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        RaiseDomainEvent(new CategoryParentChangedEvent(Id, previousParent, newParentCategoryId));
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        RaiseDomainEvent(new CategoryActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        RaiseDomainEvent(new CategoryDeactivatedEvent(Id));
    }
}