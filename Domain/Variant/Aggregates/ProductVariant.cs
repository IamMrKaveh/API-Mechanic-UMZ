using Domain.Product.Exceptions;
using Domain.Product.ValueObjects;
using Domain.Shipping.ValueObjects;
using Domain.Variant.Entities;
using Domain.Variant.Events;
using Domain.Variant.Exceptions;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Aggregates;

public sealed class ProductVariant : AggregateRoot<VariantId>, ISoftDeletable
{
    private ProductVariant()
    { }

    public Product.Aggregates.Product Product { get; private set; } = default!;
    public ProductId ProductId { get; private set; } = default!;

    public Sku Sku { get; private set; } = default!;
    public Money OriginalPrice { get; private set; } = default!;
    public Money SellingPrice { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public IReadOnlyList<VariantAttribute> Attributes => _attributes.AsReadOnly();
    private readonly List<VariantAttribute> _attributes = [];
    public IReadOnlyList<VariantShipping> Shippings => _shippings.AsReadOnly();
    private readonly List<VariantShipping> _shippings = [];

    public static ProductVariant Create(
        VariantId id,
        ProductId productId,
        Sku sku,
        Money sellingPrice,
        Money? originalPrice = null)
    {
        Guard.Against.Null(id, nameof(id));
        Guard.Against.Null(productId, nameof(productId));
        Guard.Against.Null(sku, nameof(sku));
        Guard.Against.Null(sellingPrice, nameof(sellingPrice));

        if (sellingPrice.Amount <= 0)
            throw new InvalidPriceException("قیمت واریانت باید بزرگتر از صفر باشد.");

        var resolvedSellingPrice = Money.FromDecimal(sellingPrice.Amount, sellingPrice.Currency);
        var resolvedOriginalPrice = originalPrice is not null && originalPrice.Amount > 0
            ? Money.FromDecimal(originalPrice.Amount, originalPrice.Currency)
            : Money.FromDecimal(sellingPrice.Amount, sellingPrice.Currency);

        if (resolvedOriginalPrice.Amount < resolvedSellingPrice.Amount)
            throw new InvalidPriceException("قیمت اصلی نمی‌تواند کمتر از قیمت فروش باشد.");

        var variant = new ProductVariant
        {
            Id = id,
            ProductId = productId,
            Sku = sku,
            SellingPrice = resolvedSellingPrice,
            OriginalPrice = resolvedOriginalPrice,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        variant.RaiseDomainEvent(new VariantCreatedEvent(id, productId, sku, resolvedSellingPrice));
        return variant;
    }

    public void ChangePrice(Money sellingPrice, Money? originalPrice = null)
    {
        EnsureActive();
        Guard.Against.Null(sellingPrice, nameof(sellingPrice));

        if (sellingPrice.Amount <= 0)
            throw new InvalidPriceException("قیمت واریانت باید بزرگتر از صفر باشد.");

        var resolvedSellingPrice = Money.FromDecimal(sellingPrice.Amount, sellingPrice.Currency);
        var resolvedOriginalPrice = originalPrice is not null && originalPrice.Amount > 0
            ? Money.FromDecimal(originalPrice.Amount, originalPrice.Currency)
            : Money.FromDecimal(sellingPrice.Amount, sellingPrice.Currency);

        if (resolvedOriginalPrice.Amount < resolvedSellingPrice.Amount)
            throw new InvalidPriceException("قیمت اصلی نمی‌تواند کمتر از قیمت فروش باشد.");

        var previousPrice = SellingPrice;
        SellingPrice = resolvedSellingPrice;
        OriginalPrice = resolvedOriginalPrice;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ProductVariantPriceChangedEvent(Id, ProductId, previousPrice, SellingPrice));
    }

    public void ChangeSku(Sku newSku)
    {
        EnsureActive();
        Guard.Against.Null(newSku, nameof(newSku));
        Sku = newSku;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAttributes(IEnumerable<AttributeAssignment> assignments)
    {
        EnsureActive();

        var desired = (assignments ?? Enumerable.Empty<AttributeAssignment>()).ToList();

        var desiredByValueId = desired
            .GroupBy(a => a.ValueId)
            .ToDictionary(g => g.Key, g => g.First());

        var toRemove = _attributes
            .Where(existing => !desiredByValueId.ContainsKey(existing.ValueId))
            .ToList();

        foreach (var orphan in toRemove)
            _attributes.Remove(orphan);

        foreach (var existing in _attributes)
        {
            if (desiredByValueId.TryGetValue(existing.ValueId, out var match))
                existing.UpdateDisplay(match.AttributeId, match.DisplayValue);
        }

        var existingValueIds = _attributes.Select(a => a.ValueId).ToHashSet();

        foreach (var assignment in desired)
        {
            if (existingValueIds.Contains(assignment.ValueId))
                continue;

            _attributes.Add(VariantAttribute.Create(
                Id,
                assignment.AttributeId,
                assignment.ValueId,
                assignment.DisplayValue));

            existingValueIds.Add(assignment.ValueId);
        }

        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new VariantAttributeSetEvent(Id, ProductId));
    }

    public void SetShippingMethods(decimal shippingMultiplier, IEnumerable<ShippingAssignment> assignments)
    {
        EnsureActive();

        if (shippingMultiplier <= 0)
            throw new DomainException("ضریب هزینه ارسال باید بزرگتر از صفر باشد.");

        var desired = (assignments ?? Enumerable.Empty<ShippingAssignment>()).ToList();

        var desiredByShippingId = desired
            .GroupBy(a => a.ShippingId)
            .ToDictionary(g => g.Key, g => g.First());

        var toRemove = _shippings
            .Where(existing => !desiredByShippingId.ContainsKey(existing.ShippingId))
            .ToList();

        foreach (var orphan in toRemove)
            _shippings.Remove(orphan);

        foreach (var existing in _shippings)
        {
            if (desiredByShippingId.TryGetValue(existing.ShippingId, out var match))
                existing.UpdateDimensions(
                    match.Weight,
                    match.Width,
                    match.Height,
                    match.Length,
                    shippingMultiplier);
        }

        var existingShippingIds = _shippings.Select(s => s.ShippingId).ToHashSet();

        foreach (var assignment in desired)
        {
            if (existingShippingIds.Contains(assignment.ShippingId))
                continue;

            _shippings.Add(VariantShipping.Create(
                Id,
                assignment.ShippingId,
                assignment.Weight,
                assignment.Width,
                assignment.Height,
                assignment.Length,
                shippingMultiplier));

            existingShippingIds.Add(assignment.ShippingId);
        }

        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new VariantShippingSetEvent(Id, ProductId));
    }

    public void Remove(Guid? deletedBy = null)
    {
        IsActive = false;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new VariantRemovedEvent(ProductId, Id));
    }

    public bool IsDiscounted =>
        OriginalPrice.Amount > SellingPrice.Amount;

    public decimal? DiscountPercentage
    {
        get
        {
            if (!IsDiscounted || OriginalPrice.Amount == 0)
                return null;
            return Math.Round(
                (OriginalPrice.Amount - SellingPrice.Amount) / OriginalPrice.Amount * 100, 2);
        }
    }

    private void EnsureActive()
    {
        if (!IsActive || IsDeleted)
            throw new InvalidVariantOperationException(
                Id, "تغییر", "عملیات بر روی واریانت غیرفعال یا حذف‌شده مجاز نیست.");
    }
}