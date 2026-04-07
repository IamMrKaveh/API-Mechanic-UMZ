using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.Events;
using Domain.Product.ValueObjects;

namespace Domain.Product.Aggregates;

public sealed class Product : AggregateRoot<ProductId>
{
    private Product()
    { }

    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public CategoryId CategoryId { get; private set; } = default!;
    public BrandId BrandId { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Product Create(
        ProductId id,
        ProductName name,
        Slug slug,
        string description,
        CategoryId categoryId,
        BrandId brandId)
    {
        var product = new Product
        {
            Id = id,
            Name = name,
            Slug = slug,
            Description = description,
            CategoryId = categoryId,
            BrandId = brandId,
            IsActive = true,
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        product.RaiseDomainEvent(new ProductCreatedEvent(id, name, categoryId, brandId));
        return product;
    }

    public void UpdateDetails(ProductName name, Slug slug, string description)
    {
        Name = name;
        Slug = slug;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductUpdatedEvent(Id, name, slug, description));
    }

    public void ChangeCategory(CategoryId categoryId)
    {
        var previous = CategoryId;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductCategoryChangedEvent(Id, previous, categoryId));
    }

    public void ChangeBrand(BrandId brandId)
    {
        var previous = BrandId;
        BrandId = brandId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductBrandChangedEvent(Id, previous, brandId));
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductDeactivatedEvent(Id));
    }

    public void MarkAsFeatured()
    {
        if (IsFeatured)
            return;

        IsFeatured = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnmarkAsFeatured()
    {
        if (!IsFeatured)
            return;

        IsFeatured = false;
        UpdatedAt = DateTime.UtcNow;
    }
}