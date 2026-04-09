using Domain.Category.Events;
using Domain.Category.Exceptions;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Domain.Category.Aggregates;

public sealed class Category : AggregateRoot<CategoryId>
{
    public CategoryName Name { get; private set; } = default!;
    public Slug Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Category()
    { }

    public static Category Create(
        CategoryId id,
        CategoryName name,
        Slug slug,
        ICategoryUniquenessChecker uniquenessChecker,
        string? description = null,
        int sortOrder = 0)
    {
        ArgumentNullException.ThrowIfNull(uniquenessChecker);

        if (!uniquenessChecker.IsUnique(name, slug.Value))
            throw new DuplicateCategoryNameException(name);

        var category = new Category
        {
            Id = id,
            Name = name,
            Slug = slug,
            Description = description,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        category.RaiseDomainEvent(new CategoryCreatedEvent(id, name, slug));
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