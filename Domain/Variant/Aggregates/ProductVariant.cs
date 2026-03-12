namespace Domain.Variant.Aggregates;

public sealed class ProductVariant : AggregateRoot<ProductVariantId>
{
    private readonly List<ProductVariantAttribute> _attributes = [];
    private readonly List<ProductVariantShipping> _shippingMethods = [];

    private ProductVariant()
    { }

    public ProductId ProductId { get; private set; } = default!;
    public Sku Sku { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public Money? CompareAtPrice { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<ProductVariantAttribute> Attributes => _attributes.AsReadOnly();
    public IReadOnlyList<ProductVariantShipping> ShippingMethods => _shippingMethods.AsReadOnly();

    public static ProductVariant Create(
        ProductVariantId id,
        ProductId productId,
        Sku sku,
        Money price,
        Money? compareAtPrice = null)
    {
        Guard.Against.Null(id, nameof(id));
        Guard.Against.Null(productId, nameof(productId));
        Guard.Against.Null(sku, nameof(sku));
        Guard.Against.Null(price, nameof(price));

        if (price.Amount <= 0)
            throw new InvalidPriceException("قیمت واریانت باید بزرگتر از صفر باشد.");

        if (compareAtPrice is not null && compareAtPrice.Amount < price.Amount)
            throw new InvalidPriceException("قیمت مقایسه‌ای نمی‌تواند کمتر از قیمت فروش باشد.");

        var variant = new ProductVariant
        {
            Id = id,
            ProductId = productId,
            Sku = sku,
            Price = price,
            CompareAtPrice = compareAtPrice,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        variant.RaiseDomainEvent(new ProductVariantCreatedEvent(id, productId, sku.Value, price));
        return variant;
    }

    public void ChangePrice(Money newPrice, Money? newCompareAtPrice = null)
    {
        EnsureNotDeleted();

        Guard.Against.Null(newPrice, nameof(newPrice));

        if (newPrice.Amount <= 0)
            throw new InvalidPriceException("قیمت واریانت باید بزرگتر از صفر باشد.");

        if (newCompareAtPrice is not null && newCompareAtPrice.Amount < newPrice.Amount)
            throw new InvalidPriceException("قیمت مقایسه‌ای نمی‌تواند کمتر از قیمت فروش باشد.");

        var previousPrice = Price;
        Price = newPrice;
        CompareAtPrice = newCompareAtPrice;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductVariantPriceChangedEvent(Id, ProductId, previousPrice, newPrice));
    }

    public void ChangeSku(Sku newSku)
    {
        EnsureNotDeleted();
        Guard.Against.Null(newSku, nameof(newSku));

        Sku = newSku;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAttributes(IEnumerable<AttributeAssignment> assignments)
    {
        EnsureNotDeleted();

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
        EnsureNotDeleted();

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
        EnsureNotDeleted();

        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductVariantActivatedEvent(Id, ProductId));
    }

    public void Deactivate()
    {
        EnsureNotDeleted();

        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductVariantDeactivatedEvent(Id, ProductId));
    }

    public void Delete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductVariantRemovedEvent(ProductId.Value.GetHashCode(), Id.Value.GetHashCode()));
    }

    public bool IsDiscounted => CompareAtPrice is not null && CompareAtPrice.Amount > Price.Amount;

    public decimal? DiscountPercentage
    {
        get
        {
            if (!IsDiscounted || CompareAtPrice is null || CompareAtPrice.Amount == 0)
                return null;
            return Math.Round((CompareAtPrice.Amount - Price.Amount) / CompareAtPrice.Amount * 100, 2);
        }
    }

    public bool SkuMatches(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return false;
        return Sku.Value.Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidVariantOperationException(Id.Value.GetHashCode(), "تغییر", "واریانت حذف شده است.");
    }
}