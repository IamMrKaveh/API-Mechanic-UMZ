using Domain.Category.Events;
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
        string name,
        Slug slug,
        string? description = null,
        CategoryId? parentCategoryId = null,
        int sortOrder = 0)
    {
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

        category.RaiseDomainEvent(new CategoryCreatedEvent(id, name, slug.Value, parentCategoryId));
        return category;
    }

    public void UpdateDetails(string name, Slug slug, string? description, int sortOrder)
    {
        Name = name;
        Slug = slug;
        Description = description;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CategoryUpdatedEvent(Id, name, slug.Value, description));
    }

    public void MoveToParent(CategoryId? newParentCategoryId)
    {
        var previousParent = ParentCategoryId;
        ParentCategoryId = newParentCategoryId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CategoryParentChangedEvent(Id, previousParent, newParentCategoryId));
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CategoryActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new CategoryDeactivatedEvent(Id));
    }
}