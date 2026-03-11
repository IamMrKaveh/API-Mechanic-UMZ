namespace Domain.Variant.Aggregates;

public sealed class ProductVariant : AggregateRoot<ProductVariantId>
{
    private readonly List<ProductVariantAttribute> _attributes = [];
    private readonly List<ProductVariantShipping> _shippingMethods = [];

    private ProductVariant()
    { }

    public ProductId ProductId { get; private set; } = default!;
    public string Sku { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public Money? CompareAtPrice { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<ProductVariantAttribute> Attributes => _attributes.AsReadOnly();
    public IReadOnlyList<ProductVariantShipping> ShippingMethods => _shippingMethods.AsReadOnly();

    public static ProductVariant Create(
        ProductVariantId id,
        ProductId productId,
        string sku,
        Money price,
        Money? compareAtPrice = null)
    {
        var variant = new ProductVariant
        {
            Id = id,
            ProductId = productId,
            Sku = sku,
            Price = price,
            CompareAtPrice = compareAtPrice,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        variant.RaiseDomainEvent(new ProductVariantCreatedEvent(id, productId, sku, price));
        return variant;
    }

    public void ChangePrice(Money newPrice, Money? newCompareAtPrice = null)
    {
        var previousPrice = Price;
        Price = newPrice;
        CompareAtPrice = newCompareAtPrice;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductVariantPriceChangedEvent(Id, ProductId, previousPrice, newPrice));
    }

    public void SetAttributes(IEnumerable<AttributeAssignment> assignments)
    {
        _attributes.Clear();

        foreach (var assignment in assignments)
        {
            _attributes.Add(ProductVariantAttribute.Create(
                Id,
                assignment.AttributeId,
                assignment.ValueId,
                assignment.DisplayValue));
        }

        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductVariantAttributeSetEvent(Id, ProductId));
    }

    public void SetShippingMethods(IEnumerable<ShippingAssignment> assignments)
    {
        _shippingMethods.Clear();

        foreach (var assignment in assignments)
        {
            _shippingMethods.Add(ProductVariantShipping.Create(
                Id,
                assignment.ShippingId,
                assignment.Weight,
                assignment.Width,
                assignment.Height,
                assignment.Length));
        }

        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductVariantShippingSetEvent(Id, ProductId));
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductVariantActivatedEvent(Id, ProductId));
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductVariantDeactivatedEvent(Id, ProductId));
    }
}