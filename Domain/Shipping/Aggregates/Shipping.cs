using Domain.Shipping.Events;
using Domain.Shipping.Exceptions;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Shipping.Aggregates;

public sealed class Shipping : AggregateRoot<ShippingId>, IActivatable, IAuditable
{
    public ShippingName Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Money BaseCost { get; private set; } = null!;
    public string? EstimatedDeliveryTime { get; private set; }
    public DeliveryTimeRange DeliveryTime { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }
    public bool IsDefault { get; private set; }
    public ShippingOrderRange OrderRange { get; private set; } = null!;
    public decimal? MaxWeight { get; private set; }
    public FreeShippingThreshold FreeShipping { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Money Cost => BaseCost;

    private Shipping()
    { }

    public static Shipping Create(
        ShippingName name,
        Money baseCost,
        string? description = null,
        string? estimatedDeliveryTime = null,
        int minDeliveryDays = 1,
        int maxDeliveryDays = 7)
    {
        Guard.Against.Null(name, nameof(name));
        Guard.Against.Null(baseCost, nameof(baseCost));

        var shippingId = ShippingId.NewId();
        var deliveryTime = DeliveryTimeRange.Create(minDeliveryDays, maxDeliveryDays);

        var shipping = new Shipping
        {
            Id = shippingId,
            Name = name,
            BaseCost = baseCost,
            Description = description?.Trim(),
            EstimatedDeliveryTime = estimatedDeliveryTime?.Trim(),
            DeliveryTime = deliveryTime,
            IsActive = true,
            SortOrder = 0,
            IsDefault = false,
            OrderRange = ShippingOrderRange.Unlimited(),
            FreeShipping = FreeShippingThreshold.Disabled(),
            CreatedAt = DateTime.UtcNow
        };

        shipping.RaiseDomainEvent(new ShippingCreatedEvent(shippingId, name, baseCost.Amount));
        return shipping;
    }

    public void Update(
        ShippingName name,
        Money baseCost,
        string? description,
        string? estimatedDeliveryTime,
        int minDeliveryDays,
        int maxDeliveryDays)
    {
        Guard.Against.Null(name, nameof(name));
        Guard.Against.Null(baseCost, nameof(baseCost));

        var previousCost = BaseCost.Amount;
        var deliveryTime = DeliveryTimeRange.Create(minDeliveryDays, maxDeliveryDays);

        Name = name;
        BaseCost = baseCost;
        Description = description?.Trim();
        EstimatedDeliveryTime = estimatedDeliveryTime?.Trim();
        DeliveryTime = deliveryTime;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ShippingUpdatedEvent(Id, name));

        if (previousCost != baseCost.Amount)
            RaiseDomainEvent(new ShippingCostChangedEvent(Id, previousCost, baseCost.Amount));
    }

    public void SetOrderRange(Money? minOrderAmount, Money? maxOrderAmount)
    {
        OrderRange = ShippingOrderRange.Create(minOrderAmount, maxOrderAmount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMaxWeight(decimal? maxWeight)
    {
        if (maxWeight.HasValue && maxWeight.Value <= 0)
            throw new DomainException("حداکثر وزن باید بزرگتر از صفر باشد.");

        MaxWeight = maxWeight;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableFreeShipping(Money thresholdAmount)
    {
        FreeShipping = FreeShippingThreshold.Create(thresholdAmount);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ShippingFreeThresholdChangedEvent(Id, true, thresholdAmount.Amount));
    }

    public void DisableFreeShipping()
    {
        FreeShipping = FreeShippingThreshold.Disabled();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ShippingFreeThresholdChangedEvent(Id, false, null));
    }

    public Money CalculateCost(Money orderTotal, decimal shippingMultiplier = 1m)
    {
        if (!IsActive) return Money.Zero();

        if (FreeShipping.QualifiesForFreeShipping(orderTotal))
            return Money.Zero();

        var cost = BaseCost.Amount * shippingMultiplier;
        return Money.FromDecimal(Math.Round(cost, 0));
    }

    public Money CalculateCostForCart(
        Money orderTotal,
        IEnumerable<ShippingCostItem> items)
    {
        if (!IsActive) return Money.Zero();

        if (FreeShipping.QualifiesForFreeShipping(orderTotal))
            return Money.Zero();

        var totalMultiplier = 0m;
        var totalQuantity = 0;

        foreach (var item in items)
        {
            totalMultiplier += item.ShippingMultiplier * item.Quantity;
            totalQuantity += item.Quantity;
        }

        var avgMultiplier = totalQuantity > 0 ? totalMultiplier / totalQuantity : 1m;
        var cost = BaseCost.Amount * avgMultiplier;

        return Money.FromDecimal(Math.Round(cost, 0));
    }

    public bool IsAvailableForOrder(Money orderTotal)
    {
        if (!IsActive) return false;
        return OrderRange.IsInRange(orderTotal);
    }

    public Result ValidateForOrder(Money orderTotal)
    {
        if (!IsActive)
            return Result.Failure(new Error(
                "400",
                "روش ارسال غیرفعال است.",
                ErrorType.Validation));

        return OrderRange.Validate(orderTotal);
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ShippingActivatedEvent(Id, Name));
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        if (IsDefault)
            throw new DefaultShippingCannotBeDeactivatedException(Id);

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ShippingDeactivatedEvent(Id, Name));
    }

    public void SetAsDefault()
    {
        if (!IsActive)
            throw new DomainException("امکان تنظیم روش ارسال غیرفعال به عنوان پیش‌فرض وجود ندارد.");

        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ShippingSetAsDefaultEvent(Id, Name));
    }

    public void UnsetDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
            throw new DomainException("ترتیب نمایش نمی‌تواند منفی باشد.");

        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RequestDeletion(UserId? deletedBy = null)
    {
        if (IsDefault)
            throw new DefaultShippingCannotBeDeletedException(Id);

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ShippingDeletedEvent(Id, deletedBy));
    }

    public string GetDeliveryTimeDisplay()
    {
        return DeliveryTime.ToDisplayString(EstimatedDeliveryTime);
    }

    public bool QualifiesForFreeShipping(Money orderTotal)
    {
        return FreeShipping.QualifiesForFreeShipping(orderTotal);
    }

    public void Restore()
    {
        throw new NotImplementedException();
    }
}