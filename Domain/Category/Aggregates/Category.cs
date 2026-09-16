using Domain.Brand.ValueObjects;
using Domain.Category.Events;
using Domain.Category.Exceptions;
using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Domain.Category.Aggregates;

public sealed class Category : AggregateRoot<CategoryId>
{
    public CategoryName Name { get; private set; } = default!;
    public CategorySlug Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<BrandId> _brands = [];
    public IReadOnlyCollection<BrandId> Brands => _brands;

    private Category()
    { }

    public static async Task<Category> Create(
        CategoryId id,
        CategoryName name,
        CategorySlug slug,
        ICategoryUniquenessChecker uniquenessChecker,
        string? description,
        int sortOrder,
        DateTime now,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(uniquenessChecker);

        if (!await uniquenessChecker.IsUniqueAsync(name, slug, null, ct))
            throw new DuplicateCategoryNameException(name);

        var category = new Category
        {
            Id = id,
            Name = name,
            Slug = slug,
            Description = description,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

        category.RaiseDomainEvent(new CategoryCreatedEvent(id, name, slug));
        return category;
    }

    public async Task UpdateDetails(
        CategoryName name,
        CategorySlug slug,
        ICategoryUniquenessChecker? uniquenessChecker,
        string? description,
        int sortOrder,
        DateTime now,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(uniquenessChecker);

        if (!await uniquenessChecker.IsUniqueAsync(name, slug, Id, ct))
            throw new DuplicateCategoryNameException(name);

        Name = name;
        Slug = slug;
        Description = description;
        SortOrder = sortOrder;
        UpdatedAt = now;
        IncrementVersion();

        RaiseDomainEvent(new CategoryUpdatedEvent(Id, name, slug, description));
    }

    public void Activate(DateTime now)
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = now;
        IncrementVersion();

        RaiseDomainEvent(new CategoryActivatedEvent(Id));
    }

    public void Deactivate(DateTime now)
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = now;
        IncrementVersion();

        RaiseDomainEvent(new CategoryDeactivatedEvent(Id));
    }
}