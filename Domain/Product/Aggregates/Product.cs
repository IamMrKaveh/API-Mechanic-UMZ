using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.Events;
using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;

namespace Domain.Product.Aggregates;

public sealed class Product : AggregateRoot<ProductId>, ISoftDeletable
{
    private Product()
    { }

    public ProductName Name { get; private set; } = default!;
    public ProductSlug Slug { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public Brand.Aggregates.Brand Brand { get; private set; } = default!;
    public BrandId BrandId { get; private set; } = default!;

    public Category.Aggregates.Category Category { get; private set; } = default!;
    public CategoryId CategoryId { get; private set; } = default!;

    private readonly List<ProductVariant> _variants = [];
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public static Product Create(
        ProductName name,
        ProductSlug slug,
        string description,
        BrandId brandId,
        CategoryId categoryId)
    {
        var product = new Product
        {
            Id = ProductId.NewId(),
            Name = name,
            Slug = slug,
            Description = description ?? string.Empty,
            BrandId = brandId,
            CategoryId = categoryId,
            IsActive = true,
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        product.RaiseDomainEvent(new ProductCreatedEvent(product.Id, name, brandId, categoryId));
        return product;
    }

    public void UpdateDetails(ProductName name, ProductSlug slug, string description)
    {
        Name = name;
        Slug = slug;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductUpdatedEvent(Id, name, slug, description));
    }

    public void ChangeBrand(BrandId brandId)
    {
        if (BrandId == brandId) return;
        var previous = BrandId;
        BrandId = brandId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductBrandChangedEvent(Id, previous, brandId));
    }

    public void ChangeCategory(CategoryId newCategoryId)
    {
        if (CategoryId == newCategoryId) return;
        var previous = CategoryId;
        CategoryId = newCategoryId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductCategoryChangedEvent(Id, previous, newCategoryId));
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductDeactivatedEvent(Id));
    }

    public void MarkAsFeatured()
    {
        if (IsFeatured) return;
        IsFeatured = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnmarkAsFeatured()
    {
        if (!IsFeatured) return;
        IsFeatured = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}