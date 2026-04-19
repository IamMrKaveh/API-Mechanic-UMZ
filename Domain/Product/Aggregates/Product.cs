using Domain.Brand.ValueObjects;
using Domain.Product.Events;
using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;

namespace Domain.Product.Aggregates;

public sealed class Product : AggregateRoot<ProductId>, ISoftDeletable
{
    private Product()
    { }

    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
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
    private readonly List<ProductVariant> _variants = [];
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public static Product Create(
        ProductName name,
        Slug slug,
        string description,
        BrandId brandId)
    {
        var product = new Product
        {
            Id = ProductId.NewId(),
            Name = name,
            Slug = slug,
            Description = description,
            BrandId = brandId,
            IsActive = true,
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        product.RaiseDomainEvent(new ProductCreatedEvent(product.Id, name, brandId));
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