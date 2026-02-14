namespace Domain.Product;

public class ProductVariant : BaseEntity, ISoftDeletable, IAuditable, IActivatable
{
    private readonly List<ProductVariantAttribute> _variantAttributes = new();
    private readonly List<ProductVariantShippingMethod> _shippingMethods = new();
    private readonly List<InventoryTransaction> _inventoryTransactions = new();

    public int ProductId { get; private set; }

    public string? Sku { get; private set; }
    public decimal PurchasePrice { get; private set; }
    public decimal OriginalPrice { get; private set; }
    public decimal SellingPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public int Stock => StockQuantity;
    public int ReservedQuantity { get; private set; }
    public bool IsUnlimited { get; private set; }
    public decimal ShippingMultiplier { get; private set; } = 1m;
    public bool IsActive { get; private set; } = true;
    public int LowStockThreshold { get; private set; } = 5;

    // Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public Product? Product { get; private set; }

    public ICollection<Media.Media> Images { get; private set; } = [];
    public ICollection<OrderItem> OrderItems { get; private set; } = [];

    public IReadOnlyCollection<ProductVariantAttribute> VariantAttributes => _variantAttributes.AsReadOnly();
    public IReadOnlyCollection<ProductVariantShippingMethod> ShippingMethods => _shippingMethods.AsReadOnly();
    public IReadOnlyCollection<InventoryTransaction> InventoryTransactions => _inventoryTransactions.AsReadOnly();

    /// <summary>
    /// Navigation alias برای EF Core — معادل ShippingMethods
    /// </summary>
    public IReadOnlyCollection<ProductVariantShippingMethod> ProductVariantShippingMethods => _shippingMethods.AsReadOnly();

    /// <summary>
    /// Navigation برای EF Core - آیتم‌های سبد خرید مرتبط با این واریانت
    /// </summary>
    public ICollection<Cart.CartItem> CartItems { get; private set; } = [];

    // Computed Properties
    public int AvailableStock => IsUnlimited ? int.MaxValue : Math.Max(0, StockQuantity - ReservedQuantity);

    public bool IsInStock => IsUnlimited || AvailableStock > 0;
    public bool IsLowStock => !IsUnlimited && AvailableStock <= LowStockThreshold && AvailableStock > 0;
    public bool IsOutOfStock => !IsUnlimited && AvailableStock <= 0;
    public bool HasDiscount => OriginalPrice > SellingPrice;
    public decimal DiscountPercentage => HasDiscount ? Math.Round((1 - (SellingPrice / OriginalPrice)) * 100, 2) : 0;
    public decimal ProfitMargin => PurchasePrice > 0 ? Math.Round(((SellingPrice - PurchasePrice) / PurchasePrice) * 100, 2) : 0;
    public Money ProfitAmount => Money.FromDecimal(SellingPrice - PurchasePrice);

    /// <summary>
    /// نام نمایشی واریانت — خلاصه ویژگی‌ها یا SKU
    /// </summary>
    public string DisplayName => !string.IsNullOrEmpty(GetAttributesSummary())
        ? GetAttributesSummary()
        : Sku ?? $"واریانت #{Id}";

    // Business Constants
    private const decimal MinShippingMultiplier = 0.1m;

    private const decimal MaxShippingMultiplier = 10m;
    private const int MaxStockQuantity = 1_000_000;

    private ProductVariant()
    { }

    #region Factory Methods

    public static ProductVariant Create(
        Product product,
        string? sku,
        decimal purchasePrice,
        decimal sellingPrice,
        decimal originalPrice,
        int stock,
        bool isUnlimited,
        decimal shippingMultiplier)
    {
        Guard.Against.Null(product, nameof(product));

        ValidatePricing(purchasePrice, sellingPrice, originalPrice);
        ValidateStock(stock, isUnlimited);
        ValidateShippingMultiplier(shippingMultiplier);

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Product = product,
            Sku = NormalizeSku(sku),
            PurchasePrice = purchasePrice,
            SellingPrice = sellingPrice,
            OriginalPrice = originalPrice,
            StockQuantity = stock,
            ReservedQuantity = 0,
            IsUnlimited = isUnlimited,
            ShippingMultiplier = shippingMultiplier,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return variant;
    }

    #endregion Factory Methods

    #region Update Methods

    /// <summary>
    /// بروزرسانی جزئیات واریانت — قابل دسترسی از لایه Application
    /// </summary>
    public void UpdateDetails(string? sku, decimal shippingMultiplier)
    {
        EnsureNotDeleted();
        ValidateShippingMultiplier(shippingMultiplier);

        Sku = NormalizeSku(sku);
        ShippingMultiplier = shippingMultiplier;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePrices(decimal purchasePrice, decimal sellingPrice, decimal originalPrice)
    {
        EnsureNotDeleted();
        ValidatePricing(purchasePrice, sellingPrice, originalPrice);

        PurchasePrice = purchasePrice;
        SellingPrice = sellingPrice;
        OriginalPrice = originalPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyDiscount(decimal discountPercentage)
    {
        EnsureNotDeleted();

        if (discountPercentage < 0 || discountPercentage > 100)
            throw new DomainException("درصد تخفیف باید بین ۰ تا ۱۰۰ باشد.");

        var discountedPrice = OriginalPrice * (1 - discountPercentage / 100);
        discountedPrice = Math.Round(discountedPrice, 0);

        if (discountedPrice < PurchasePrice)
            throw new DomainException("قیمت پس از تخفیف نمی‌تواند کمتر از قیمت خرید باشد.");

        SellingPrice = discountedPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveDiscount()
    {
        EnsureNotDeleted();

        SellingPrice = OriginalPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLowStockThreshold(int threshold)
    {
        if (threshold < 0)
            throw new DomainException("آستانه کم‌موجودی نمی‌تواند منفی باشد.");

        LowStockThreshold = threshold;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetUnlimited(bool isUnlimited)
    {
        EnsureNotDeleted();

        IsUnlimited = isUnlimited;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Update Methods

    #region Stock Management - public, Called by Product

    public void AdjustStock(int quantityChange)
    {
        EnsureNotDeleted();

        if (IsUnlimited) return;

        var newStock = StockQuantity + quantityChange;

        if (newStock < 0)
            throw new DomainException($"موجودی نمی‌تواند منفی شود. موجودی فعلی: {StockQuantity}، تغییر: {quantityChange}");

        StockQuantity = newStock;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStock(int quantity)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (IsUnlimited)
            return;

        var newStock = StockQuantity + quantity;
        if (newStock > MaxStockQuantity)
            throw new DomainException($"موجودی نمی‌تواند بیش از {MaxStockQuantity:N0} باشد.");

        StockQuantity = newStock;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReduceStock(int quantity)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (IsUnlimited)
            return;

        if (AvailableStock < quantity)
            throw new DomainException($"موجودی کافی نیست. موجودی قابل فروش: {AvailableStock}، درخواستی: {quantity}");

        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStock(int quantity)
    {
        EnsureNotDeleted();

        if (quantity < 0)
            throw new DomainException("موجودی نمی‌تواند منفی باشد.");

        if (quantity > MaxStockQuantity)
            throw new DomainException($"موجودی نمی‌تواند بیش از {MaxStockQuantity:N0} باشد.");

        if (IsUnlimited)
            return;

        StockQuantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reserve(int quantity)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (IsUnlimited)
            return;

        if (AvailableStock < quantity)
            throw new DomainException($"موجودی کافی برای رزرو نیست. موجودی قابل رزرو: {AvailableStock}");

        ReservedQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release(int quantity)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (IsUnlimited)
            return;

        var releaseAmount = Math.Min(quantity, ReservedQuantity);
        ReservedQuantity -= releaseAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmReservation(int quantity)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (IsUnlimited)
            return;

        if (ReservedQuantity < quantity)
            throw new DomainException($"موجودی رزرو شده کافی نیست. رزرو شده: {ReservedQuantity}، درخواستی: {quantity}");

        ReservedQuantity -= quantity;
        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Stock Management - public, Called by Product

    #region Query Methods

    public bool CanFulfill(int quantity)
    {
        if (IsUnlimited)
            return true;

        return AvailableStock >= quantity;
    }

    public string GetAttributesSummary()
    {
        if (!_variantAttributes.Any())
            return string.Empty;

        return string.Join(" - ", _variantAttributes
            .OrderBy(va => va.AttributeValue?.AttributeType?.SortOrder ?? 0)
            .Select(va => va.AttributeValue?.DisplayValue ?? ""));
    }

    public bool SupportsShippingMethod(int shippingMethodId)
    {
        return _shippingMethods.Any(sm => sm.ShippingMethodId == shippingMethodId && sm.IsActive);
    }

    #endregion Query Methods

    #region Attribute Management - public

    public void AddAttribute(AttributeValue attributeValue)
    {
        EnsureNotDeleted();
        Guard.Against.Null(attributeValue, nameof(attributeValue));

        if (_variantAttributes.Any(va => va.AttributeValueId == attributeValue.Id))
            throw new DomainException("این ویژگی قبلاً به واریانت اضافه شده است.");

        if (_variantAttributes.Any(va => va.AttributeValue?.AttributeTypeId == attributeValue.AttributeTypeId))
            throw new DomainException("هر واریانت فقط می‌تواند یک مقدار برای هر نوع ویژگی داشته باشد.");

        var variantAttribute = new ProductVariantAttribute
        {
            VariantId = Id,
            Variant = this,
            AttributeValueId = attributeValue.Id,
            AttributeValue = attributeValue
        };

        _variantAttributes.Add(variantAttribute);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveAttribute(int attributeValueId)
    {
        EnsureNotDeleted();

        var attribute = _variantAttributes.FirstOrDefault(va => va.AttributeValueId == attributeValueId);
        if (attribute == null)
            throw new DomainException("ویژگی مورد نظر در واریانت یافت نشد.");

        _variantAttributes.Remove(attribute);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearAttributes()
    {
        _variantAttributes.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Attribute Management - public

    #region Shipping Method Management - public

    public void AddShippingMethod(ShippingMethod shippingMethod)
    {
        EnsureNotDeleted();
        Guard.Against.Null(shippingMethod, nameof(shippingMethod));

        if (_shippingMethods.Any(sm => sm.ShippingMethodId == shippingMethod.Id))
            return;

        var variantShipping = new ProductVariantShippingMethod
        {
            ProductVariantId = Id,
            ProductVariant = this,
            ShippingMethodId = shippingMethod.Id,
            ShippingMethod = shippingMethod,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _shippingMethods.Add(variantShipping);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveShippingMethod(int shippingMethodId)
    {
        EnsureNotDeleted();

        var shipping = _shippingMethods.FirstOrDefault(sm => sm.ShippingMethodId == shippingMethodId);
        if (shipping == null)
            return;

        _shippingMethods.Remove(shipping);
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Shipping Method Management - public

    #region Activation & Deletion - public

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

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDeleted(int? deletedBy = null)
    {
        if (IsDeleted) return;

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

    #endregion Activation & Deletion - public

    #region Domain Invariants

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("واریانت حذف شده است.");
    }

    private static void ValidatePricing(decimal purchasePrice, decimal sellingPrice, decimal originalPrice)
    {
        if (purchasePrice < 0)
            throw new DomainException("قیمت خرید نمی‌تواند منفی باشد.");

        if (sellingPrice < 0)
            throw new DomainException("قیمت فروش نمی‌تواند منفی باشد.");

        if (originalPrice < 0)
            throw new DomainException("قیمت اصلی نمی‌تواند منفی باشد.");

        if (sellingPrice < purchasePrice)
            throw new DomainException("قیمت فروش نمی‌تواند کمتر از قیمت خرید باشد.");

        if (originalPrice > 0 && sellingPrice > originalPrice)
            throw new DomainException("قیمت فروش نمی‌تواند بیشتر از قیمت اصلی باشد.");
    }

    private static void ValidateStock(int stock, bool isUnlimited)
    {
        if (!isUnlimited && stock < 0)
            throw new DomainException("موجودی نمی‌تواند منفی باشد.");

        if (stock > MaxStockQuantity)
            throw new DomainException($"موجودی نمی‌تواند بیش از {MaxStockQuantity:N0} باشد.");
    }

    private static void ValidateShippingMultiplier(decimal multiplier)
    {
        if (multiplier < MinShippingMultiplier)
            throw new DomainException($"ضریب ارسال باید حداقل {MinShippingMultiplier} باشد.");

        if (multiplier > MaxShippingMultiplier)
            throw new DomainException($"ضریب ارسال نمی‌تواند بیشتر از {MaxShippingMultiplier} باشد.");
    }

    private static string? NormalizeSku(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        return sku.Trim().ToUpperInvariant();
    }

    #endregion Domain Invariants
}