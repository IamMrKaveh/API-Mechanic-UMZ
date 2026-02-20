namespace Domain.Product;

public class Product : AggregateRoot, IAuditable, ISoftDeletable
{
    public ProductName Name { get; private set; } = null!;
    public Slug Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }
    public int CategoryId { get; private set; }
    public int BrandId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    private readonly List<ProductVariant> _variants = new();
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    private readonly Brand.Brand _brand = new();
    public Brand.Brand Brand => _brand;

    public ICollection<Media.Media> Images { get; private set; } = new List<Media.Media>();

    public ProductStats Stats { get; private set; }

    public ProductVariant? FindVariant(int variantId) => _variants.FirstOrDefault(v => v.Id == variantId);

    public ProductVariant AddVariant(string? sku, decimal purchasePrice, decimal sellingPrice, decimal originalPrice, int stock, bool isUnlimited, decimal shippingMultiplier, List<AttributeValue> attributes)
    {
        var variant = ProductVariant.Create(Id, sku ?? "", Money.FromDecimal(purchasePrice), Money.FromDecimal(sellingPrice), Money.FromDecimal(originalPrice), stock, isUnlimited, shippingMultiplier);
        variant.AssignAttributes(attributes);
        _variants.Add(variant);
        return variant;
    }

    public void AddVariantShippingMethod(int variantId, Domain.Shipping.Shipping shippingMethod)
    {
        var variant = FindVariant(variantId);
        if (variant != null) variant.AddShipping(shippingMethod);
    }

    public void RemoveVariant(int variantId, int? deletedBy)
    {
        var variant = FindVariant(variantId);
        if (variant != null) variant.SoftDelete(deletedBy ?? 0);
    }

    public void UpdateVariantDetails(int variantId, string? sku, decimal shippingMultiplier)
    {
        var variant = FindVariant(variantId);
        if (variant != null) variant.UpdateDetails(sku, shippingMultiplier);
    }

    public void ChangeVariantPrices(int variantId, decimal purchasePrice, decimal sellingPrice, decimal originalPrice)
    {
        var variant = FindVariant(variantId);
        if (variant != null) variant.SetPricing(purchasePrice, sellingPrice, originalPrice);
    }

    public void IncreaseStock(int variantId, int quantity)
    {
        var variant = FindVariant(variantId);
        if (variant != null) variant.AddStock(quantity);
    }

    public void DecreaseStock(int variantId, int quantity)
    {
        var variant = FindVariant(variantId);
        if (variant != null) variant.AdjustStock(-quantity);
    }

    public void SetVariantUnlimited(int variantId, bool isUnlimited)
    {
        var variant = FindVariant(variantId);
        if (variant != null) variant.SetUnlimited(isUnlimited);
    }

    public void SetVariantAttributes(int variantId, List<AttributeValue> attributes)
    {
        var variant = FindVariant(variantId);
        if (variant != null) variant.AssignAttributes(attributes);
    }

    public void SetVariantShippingMethods(int variantId, IEnumerable<Domain.Shipping.Shipping> shippingMethods)
    {
        var variant = FindVariant(variantId);
        if (variant != null)
        {
            var current = variant.ProductVariantShippingMethods.ToList();
            foreach (var item in current) variant.RemoveShipping(item.ShippingId);
            foreach (var method in shippingMethods) variant.AddShipping(method);
        }
    }

    private Product()
    { }

    public static Product Create(ProductName name, Slug slug, int categoryId, int brandId, string? description)
    {
        var product = new Product
        {
            Name = name,
            Slug = slug,
            CategoryId = categoryId,
            BrandId = brandId,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Stats = ProductStats.CreateEmpty()
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name, brandId));
        return product;
    }

    public void UpdateDetails(string name, string? description, string? sku, int categoryGroupId, bool isActive)
    {
        Name = ProductName.Create(name);
        Description = description;
        CategoryId = categoryGroupId;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductUpdatedEvent(Id, Name.Value));
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ProductActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ProductDeactivatedEvent(Id));
    }

    public void Delete(int deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        AddDomainEvent(new ProductDeletedEvent(Id, deletedBy));
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStats(ProductStats newStats)
    {
        Stats = newStats;
        UpdatedAt = DateTime.UtcNow;
    }
}