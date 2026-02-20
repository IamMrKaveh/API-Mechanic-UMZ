namespace Domain.Shipping;

public class Shipping : BaseEntity, ISoftDeletable, IActivatable, IAuditable
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Money BaseCost { get; private set; } = null!;
    public string? EstimatedDeliveryTime { get; private set; }
    public int MinDeliveryDays { get; private set; }
    public int MaxDeliveryDays { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }
    public bool IsDefault { get; private set; }

    public decimal? MinOrderAmount { get; private set; }
    public decimal? MaxOrderAmount { get; private set; }
    public decimal? MaxWeight { get; private set; }
    public bool IsFreeAboveAmount { get; private set; }
    public decimal? FreeShippingThreshold { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    public ICollection<Order.Order> Orders { get; private set; } = new List<Order.Order>();
    public ICollection<ProductVariantShipping> ProductVariantShippingMethods { get; private set; } = new List<ProductVariantShipping>();

    /// <summary>
    /// Alias برای BaseCost — سازگاری با کدهایی که از Cost استفاده می‌کنند
    /// </summary>
    public Money Cost => BaseCost;

    private Shipping()
    { }

    #region Factory Methods

    public static Shipping Create(
        string name,
        Money baseCost,
        string? description = null,
        string? estimatedDeliveryTime = null,
        int minDeliveryDays = 1,
        int maxDeliveryDays = 7)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.Null(baseCost, nameof(baseCost));
        ValidateDeliveryDays(minDeliveryDays, maxDeliveryDays);

        return new Shipping
        {
            Name = name.Trim(),
            BaseCost = baseCost,
            Description = description?.Trim(),
            EstimatedDeliveryTime = estimatedDeliveryTime?.Trim(),
            MinDeliveryDays = minDeliveryDays,
            MaxDeliveryDays = maxDeliveryDays,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion Factory Methods

    #region Update Methods

    public void Update(
        string name,
        Money baseCost,
        string? description,
        string? estimatedDeliveryTime,
        int minDeliveryDays,
        int maxDeliveryDays)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.Null(baseCost, nameof(baseCost));
        ValidateDeliveryDays(minDeliveryDays, maxDeliveryDays);
        EnsureNotDeleted();

        Name = name.Trim();
        BaseCost = baseCost;
        Description = description?.Trim();
        EstimatedDeliveryTime = estimatedDeliveryTime?.Trim();
        MinDeliveryDays = minDeliveryDays;
        MaxDeliveryDays = maxDeliveryDays;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetOrderLimits(decimal? minOrderAmount, decimal? maxOrderAmount)
    {
        EnsureNotDeleted();

        if (minOrderAmount.HasValue && maxOrderAmount.HasValue && minOrderAmount > maxOrderAmount)
            throw new DomainException("حداقل مبلغ سفارش نمی‌تواند بیشتر از حداکثر باشد.");

        MinOrderAmount = minOrderAmount;
        MaxOrderAmount = maxOrderAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFreeShipping(decimal threshold)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(threshold, nameof(threshold));

        IsFreeAboveAmount = true;
        FreeShippingThreshold = threshold;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveFreeShipping()
    {
        IsFreeAboveAmount = false;
        FreeShippingThreshold = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMaxWeight(decimal maxWeight)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(maxWeight, nameof(maxWeight));

        MaxWeight = maxWeight;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault()
    {
        EnsureNotDeleted();
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Update Methods

    #region Cost Calculation

    public Money CalculateCost(Money orderTotal, decimal shippingMultiplier = 1m)
    {
        if (!IsActive || IsDeleted)
            return Money.Zero();

        if (IsFreeAboveAmount && FreeShippingThreshold.HasValue &&
            orderTotal.Amount >= FreeShippingThreshold.Value)
        {
            return Money.Zero();
        }

        var cost = BaseCost.Amount * shippingMultiplier;
        return Money.FromDecimal(Math.Round(cost, 0));
    }

    public Money CalculateCostForCart(
        Money orderTotal,
        IEnumerable<(int variantId, decimal shippingMultiplier, int quantity)> items)
    {
        if (!IsActive || IsDeleted)
            return Money.Zero();

        if (IsFreeAboveAmount && FreeShippingThreshold.HasValue &&
            orderTotal.Amount >= FreeShippingThreshold.Value)
        {
            return Money.Zero();
        }

        var totalMultiplier = 0m;
        var totalQuantity = 0;

        foreach (var item in items)
        {
            totalMultiplier += item.shippingMultiplier * item.quantity;
            totalQuantity += item.quantity;
        }

        var avgMultiplier = totalQuantity > 0 ? totalMultiplier / totalQuantity : 1m;
        var cost = BaseCost.Amount * avgMultiplier;

        return Money.FromDecimal(Math.Round(cost, 0));
    }

    #endregion Cost Calculation

    #region Validation

    public bool IsAvailableForOrder(Money orderTotal)
    {
        if (!IsActive || IsDeleted)
            return false;

        if (MinOrderAmount.HasValue && orderTotal.Amount < MinOrderAmount.Value)
            return false;

        if (MaxOrderAmount.HasValue && orderTotal.Amount > MaxOrderAmount.Value)
            return false;

        return true;
    }

    public (bool IsValid, string? Error) ValidateForOrder(Money orderTotal)
    {
        if (!IsActive)
            return (false, "روش ارسال غیرفعال است.");

        if (IsDeleted)
            return (false, "روش ارسال حذف شده است.");

        if (MinOrderAmount.HasValue && orderTotal.Amount < MinOrderAmount.Value)
            return (false, $"حداقل مبلغ سفارش برای این روش ارسال {MinOrderAmount.Value:N0} تومان است.");

        if (MaxOrderAmount.HasValue && orderTotal.Amount > MaxOrderAmount.Value)
            return (false, $"حداکثر مبلغ سفارش برای این روش ارسال {MaxOrderAmount.Value:N0} تومان است.");

        return (true, null);
    }

    #endregion Validation

    #region Activation & Deletion

    public void Activate()
    {
        if (IsActive) return;
        EnsureNotDeleted();

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        if (IsDefault)
            throw new DomainException("امکان غیرفعال کردن روش ارسال پیش‌فرض وجود ندارد.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;

        if (IsDefault)
            throw new DomainException("امکان حذف روش ارسال پیش‌فرض وجود ندارد.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
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

    #endregion Activation & Deletion

    #region Query Methods

    public string GetDeliveryTimeDisplay()
    {
        if (!string.IsNullOrWhiteSpace(EstimatedDeliveryTime))
            return EstimatedDeliveryTime;

        if (MinDeliveryDays == MaxDeliveryDays)
            return $"{MinDeliveryDays} روز کاری";

        return $"{MinDeliveryDays} تا {MaxDeliveryDays} روز کاری";
    }

    #endregion Query Methods

    #region Private Methods

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("روش ارسال حذف شده است.");
    }

    private static void ValidateDeliveryDays(int min, int max)
    {
        if (min < 0)
            throw new DomainException("حداقل روز تحویل نمی‌تواند منفی باشد.");

        if (max < min)
            throw new DomainException("حداکثر روز تحویل نمی‌تواند کمتر از حداقل باشد.");
    }

    #endregion Private Methods
}