namespace Domain.Product;

public class Product : AggregateRoot, ISoftDeletable, IAuditable, IActivatable
{
    private readonly List<ProductVariant> _variants = new();
    private readonly List<ProductReview> _reviews = new();

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Sku { get; private set; }
    public int CategoryGroupId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }

    // Calculated Aggregates
    public Money MinPrice { get; private set; } = null!;

    public Money MaxPrice { get; private set; } = null!;

    public int TotalStock { get; private set; }
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public int SalesCount { get; private set; }

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Concurrency
    public new byte[]? RowVersion { get; private set; }

    // Navigation (read-only)
    public CategoryGroup? CategoryGroup { get; private set; }

    public ICollection<Media.Media> Images { get; private set; } = [];
    public ICollection<OrderItem> OrderItems { get; private set; } = [];

    // Collections
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public IReadOnlyCollection<ProductReview> Reviews => _reviews.AsReadOnly();

    // Computed Properties
    public bool HasMultipleVariants => _variants.Count(v => !v.IsDeleted) > 1;

    public bool IsInStock => TotalStock > 0 || _variants.Any(v => !v.IsDeleted && v.IsUnlimited);
    public bool HasDiscount => _variants.Any(v => !v.IsDeleted && v.HasDiscount);

    // Business Constants
    private const int MaxVariantsPerProduct = 100;

    private Product()
    { }

    #region Factory Methods

    public static Product Create(
        string name,
        string? description,
        string? sku,
        int categoryGroupId)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NegativeOrZero(categoryGroupId, nameof(categoryGroupId));

        ValidateName(name);
        ValidateSku(sku);

        var product = new Product
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            Sku = NormalizeSku(sku),
            CategoryGroupId = categoryGroupId,
            IsActive = true,
            IsFeatured = false,
            MinPrice = Money.Zero(),
            MaxPrice = Money.Zero(),
            TotalStock = 0,
            AverageRating = 0,
            ReviewCount = 0,
            SalesCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name, product.CategoryGroupId));
        return product;
    }

    #endregion Factory Methods

    #region Product Details Management

    public void UpdateDetails(
        string name,
        string? description,
        string? sku,
        int categoryGroupId,
        bool isActive)
    {
        EnsureNotDeleted();
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NegativeOrZero(categoryGroupId, nameof(categoryGroupId));

        ValidateName(name);
        ValidateSku(sku);

        var oldCategoryGroupId = CategoryGroupId;

        Name = name.Trim();
        Description = description?.Trim();
        Sku = NormalizeSku(sku);
        CategoryGroupId = categoryGroupId;
        UpdatedAt = DateTime.UtcNow;

        if (isActive && !IsActive)
        {
            Activate();
        }
        else if (!isActive && IsActive)
        {
            Deactivate();
        }

        AddDomainEvent(new ProductUpdatedEvent(Id, Name));

        if (oldCategoryGroupId != categoryGroupId)
        {
            AddDomainEvent(new ProductCategoryChangedEvent(Id, oldCategoryGroupId, categoryGroupId));
        }
    }

    public void SetFeatured(bool isFeatured)
    {
        EnsureNotDeleted();
        if (IsFeatured == isFeatured) return;

        IsFeatured = isFeatured;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeCategory(int newCategoryGroupId)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(newCategoryGroupId, nameof(newCategoryGroupId));

        if (CategoryGroupId == newCategoryGroupId) return;

        var oldCategoryGroupId = CategoryGroupId;
        CategoryGroupId = newCategoryGroupId;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductCategoryChangedEvent(Id, oldCategoryGroupId, newCategoryGroupId));
    }

    #endregion Product Details Management

    #region Variant Management

    public ProductVariant AddVariant(
        string? sku,
        decimal purchasePrice,
        decimal sellingPrice,
        decimal originalPrice,
        int stock,
        bool isUnlimited,
        decimal shippingMultiplier,
        IEnumerable<AttributeValue> attributes)
    {
        EnsureNotDeleted();
        EnsureCanAddMoreVariants();

        if (!string.IsNullOrEmpty(sku))
        {
            var normalizedSku = NormalizeSku(sku)!;
            EnsureVariantSkuUnique(normalizedSku, excludeVariantId: null);
        }

        var attributeList = attributes.ToList();
        ValidateAttributeCombination(attributeList);

        var variant = ProductVariant.Create(
            this,
            sku,
            purchasePrice,
            sellingPrice,
            originalPrice,
            stock,
            isUnlimited,
            shippingMultiplier);

        foreach (var attr in attributeList)
        {
            variant.AddAttribute(attr);
        }

        _variants.Add(variant);
        RecalculateAggregates();

        AddDomainEvent(new ProductVariantAddedEvent(Id, variant.Id));

        return variant;
    }

    public void UpdateVariantDetails(
        int variantId,
        string? sku,
        decimal shippingMultiplier)
    {
        EnsureNotDeleted();

        var variant = GetVariantOrThrow(variantId);

        if (!string.IsNullOrEmpty(sku))
        {
            var normalizedSku = NormalizeSku(sku)!;
            EnsureVariantSkuUnique(normalizedSku, excludeVariantId: variantId);
        }

        variant.UpdateDetails(sku, shippingMultiplier);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveVariant(int variantId, int? deletedBy = null)
    {
        EnsureNotDeleted();

        var variant = GetVariantOrThrow(variantId);

        EnsureMinimumVariantsAfterRemoval();

        variant.MarkAsDeleted(deletedBy);
        RecalculateAggregates();

        AddDomainEvent(new ProductVariantRemovedEvent(Id, variantId));
    }

    #endregion Variant Management

    #region Pricing Management

    public void ChangeVariantPrices(
        int variantId,
        decimal purchasePrice,
        decimal sellingPrice,
        decimal originalPrice)
    {
        EnsureNotDeleted();

        var variant = GetVariantOrThrow(variantId);
        var oldSellingPrice = variant.SellingPrice;
        var oldOriginalPrice = variant.OriginalPrice;

        variant.ChangePrices(purchasePrice, sellingPrice, originalPrice);
        RecalculateAggregates();

        if (oldSellingPrice != sellingPrice || oldOriginalPrice != originalPrice)
        {
            AddDomainEvent(new PriceChangedEvent(
                variantId, Id, oldSellingPrice, sellingPrice, oldOriginalPrice, originalPrice));
        }
    }

    public void ApplyDiscountToVariant(int variantId, decimal discountPercentage)
    {
        EnsureNotDeleted();

        if (discountPercentage < 0 || discountPercentage > 100)
            throw new DomainException("درصد تخفیف باید بین ۰ تا ۱۰۰ باشد.");

        var variant = GetVariantOrThrow(variantId);
        var oldSellingPrice = variant.SellingPrice;

        variant.ApplyDiscount(discountPercentage);
        RecalculateAggregates();

        if (oldSellingPrice != variant.SellingPrice)
        {
            AddDomainEvent(new PriceChangedEvent(
                variantId, Id, oldSellingPrice, variant.SellingPrice, variant.OriginalPrice, variant.OriginalPrice));
        }
    }

    public void RemoveDiscountFromVariant(int variantId)
    {
        EnsureNotDeleted();

        var variant = GetVariantOrThrow(variantId);
        var oldSellingPrice = variant.SellingPrice;

        variant.RemoveDiscount();
        RecalculateAggregates();

        if (oldSellingPrice != variant.SellingPrice)
        {
            AddDomainEvent(new PriceChangedEvent(
                variantId, Id, oldSellingPrice, variant.SellingPrice, variant.OriginalPrice, variant.OriginalPrice));
        }
    }

    public Money CalculateSellingPrice(int variantId, DiscountCode? discountCode = null)
    {
        var variant = GetVariantOrThrow(variantId);

        var basePrice = Money.FromDecimal(variant.SellingPrice);

        if (discountCode == null || !discountCode.IsCurrentlyValid())
            return basePrice;

        var discountAmount = discountCode.CalculateDiscountMoney(basePrice);
        return basePrice.Subtract(discountAmount);
    }

    #endregion Pricing Management

    #region Stock Management

    public void IncreaseStock(int variantId, int quantity, string? notes = null)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        var variant = GetVariantOrThrow(variantId);

        if (variant.IsUnlimited) return;

        variant.AddStock(quantity);
        RecalculateAggregates();

        AddDomainEvent(new AdjustStockEvent(variantId, variant.StockQuantity, quantity));
    }

    public void DecreaseStock(int variantId, int quantity, string? notes = null)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        var variant = GetVariantOrThrow(variantId);

        EnsureVariantCanFulfill(variant, quantity);

        if (variant.IsUnlimited) return;

        variant.ReduceStock(quantity);
        RecalculateAggregates();

        AddDomainEvent(new AdjustStockEvent(variantId, variant.StockQuantity, -quantity));
        CheckAndRaiseStockEvents(variant);
    }

    public void SetStock(int variantId, int quantity, int? userId = null, string? notes = null)
    {
        EnsureNotDeleted();
        if (quantity < 0) throw new DomainException("موجودی نمی‌تواند منفی باشد.");

        var variant = GetVariantOrThrow(variantId);
        if (variant.IsUnlimited) return;

        var oldStock = variant.StockQuantity;
        var adjustment = quantity - oldStock;

        variant.SetStock(quantity);
        RecalculateAggregates();

        AddDomainEvent(new AdjustStockEvent(variantId, variant.StockQuantity, adjustment));
        CheckAndRaiseStockEvents(variant);
    }

    public void ReserveStock(int variantId, int quantity)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        var variant = GetVariantOrThrow(variantId);

        EnsureVariantIsAvailableForPurchase(variant);
        EnsureVariantCanFulfill(variant, quantity);

        variant.Reserve(quantity);
        RecalculateAggregates();

        AddDomainEvent(new StockReservedEvent(variantId, Id, quantity));
    }

    public void ReleaseStock(int variantId, int quantity)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        var variant = GetVariantOrThrow(variantId);
        variant.Release(quantity);
        RecalculateAggregates();

        AddDomainEvent(new StockReleasedEvent(variantId, Id, quantity));
    }

    public void ConfirmStockReservation(int variantId, int quantity)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        var variant = GetVariantOrThrow(variantId);
        variant.ConfirmReservation(quantity);
        RecalculateAggregates();

        CheckAndRaiseStockEvents(variant);
    }

    public bool CanFulfillOrder(int variantId, int quantity)
    {
        var variant = FindVariant(variantId);
        if (variant == null || variant.IsDeleted) return false;
        if (!variant.IsActive) return false;
        return variant.CanFulfill(quantity);
    }

    public void ValidateForPurchase(int variantId, int quantity)
    {
        var variant = GetVariantOrThrow(variantId);
        EnsureVariantIsAvailableForPurchase(variant);
        EnsureVariantCanFulfill(variant, quantity);
    }

    public bool IsAvailable(int? variantId = null)
    {
        if (IsDeleted || !IsActive) return false;

        if (variantId.HasValue)
        {
            var variant = FindVariant(variantId.Value);
            return variant != null && variant.IsActive && variant.IsInStock;
        }

        return _variants.Any(v => !v.IsDeleted && v.IsActive && v.IsInStock);
    }

    #endregion Stock Management

    #region Variant Activation

    public void ActivateVariant(int variantId)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.Activate();
        RecalculateAggregates();
    }

    public void DeactivateVariant(int variantId)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.Deactivate();
        RecalculateAggregates();
    }

    public void SetVariantUnlimited(int variantId, bool isUnlimited)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.SetUnlimited(isUnlimited);
        RecalculateAggregates();
    }

    public void SetVariantLowStockThreshold(int variantId, int threshold)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.SetLowStockThreshold(threshold);
    }

    #endregion Variant Activation

    #region Variant Attributes

    public void SetVariantAttributes(int variantId, IEnumerable<AttributeValue> attributes)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);

        variant.ClearAttributes();

        var attrList = attributes.ToList();

        var typeIds = attrList.Select(a => a.AttributeTypeId).ToList();
        if (typeIds.Count != typeIds.Distinct().Count())
            throw new DomainException("هر واریانت نمی‌تواند چند مقدار برای یک نوع ویژگی داشته باشد.");

        foreach (var attr in attrList)
        {
            variant.AddAttribute(attr);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void AddVariantAttribute(int variantId, AttributeValue attributeValue)
    {
        EnsureNotDeleted();
        Guard.Against.Null(attributeValue, nameof(attributeValue));

        var variant = GetVariantOrThrow(variantId);
        variant.AddAttribute(attributeValue);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveVariantAttribute(int variantId, int attributeValueId)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.RemoveAttribute(attributeValueId);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearVariantAttributes(int variantId)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.ClearAttributes();
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Variant Attributes

    #region Variant Shipping Methods

    public void SetVariantShippingMethods(int variantId, IEnumerable<ShippingMethod> shippingMethods)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);

        var existingIds = variant.ShippingMethods.Select(sm => sm.ShippingMethodId).ToList();
        foreach (var existingId in existingIds)
        {
            variant.RemoveShippingMethod(existingId);
        }

        foreach (var sm in shippingMethods)
        {
            variant.AddShippingMethod(sm);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void AddVariantShippingMethod(int variantId, ShippingMethod shippingMethod)
    {
        EnsureNotDeleted();
        Guard.Against.Null(shippingMethod, nameof(shippingMethod));

        var variant = GetVariantOrThrow(variantId);
        variant.AddShippingMethod(shippingMethod);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveVariantShippingMethod(int variantId, int shippingMethodId)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.RemoveShippingMethod(shippingMethodId);
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Variant Shipping Methods

    #region Review Management

    public ProductReview AddReview(
        int userId, int rating, string? title, string? comment,
        bool isVerifiedPurchase, int? orderId = null)
    {
        EnsureNotDeleted();

        if (_reviews.Any(r => !r.IsDeleted && r.UserId == userId && r.OrderId == orderId))
            throw new DomainException("شما قبلاً برای این محصول نظر ثبت کرده‌اید.");

        var review = ProductReview.Create(Id, userId, rating, title, comment, isVerifiedPurchase, orderId);
        _reviews.Add(review);
        RecalculateRating();

        return review;
    }

    public void ApproveReview(int reviewId)
    {
        EnsureNotDeleted();
        GetReviewOrThrow(reviewId).Approve();
        RecalculateRating();
    }

    public void RejectReview(int reviewId)
    {
        EnsureNotDeleted();
        GetReviewOrThrow(reviewId).Reject();
        RecalculateRating();
    }

    public void ReplyToReview(int reviewId, string reply)
    {
        EnsureNotDeleted();
        GetReviewOrThrow(reviewId).AddAdminReply(reply);
    }

    public void DeleteReview(int reviewId, int? deletedBy = null)
    {
        GetReviewOrThrow(reviewId).Delete(deletedBy);
        RecalculateRating();
    }

    #endregion Review Management

    #region Activation & Deletion

    public void Activate()
    {
        EnsureNotDeleted();
        if (IsActive) return;

        if (!_variants.Any(v => !v.IsDeleted))
            throw new DomainException("محصول بدون واریانت فعال قابل فعال‌سازی نیست.");

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

    public void MarkAsDeleted(int? deletedBy)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;

        foreach (var variant in _variants.Where(v => !v.IsDeleted))
        {
            variant.MarkAsDeleted(deletedBy);
        }

        AddDomainEvent(new ProductDeletedEvent(Id, deletedBy));
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        UpdatedAt = DateTime.UtcNow;

        foreach (var variant in _variants)
        {
            variant.Restore();
        }

        RecalculateAggregates();
    }

    public void IncrementSalesCount(int quantity = 1)
    {
        Guard.Against.Negative(quantity, nameof(quantity));
        SalesCount += quantity;
    }

    #endregion Activation & Deletion

    #region Query Methods

    public ProductVariant? FindVariant(int variantId)
        => _variants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted);

    public ProductVariant? FindVariantBySku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return null;
        var normalizedSku = NormalizeSku(sku);
        return _variants.FirstOrDefault(v => !v.IsDeleted && v.Sku == normalizedSku);
    }

    public IEnumerable<ProductVariant> GetActiveVariants()
        => _variants.Where(v => !v.IsDeleted && v.IsActive);

    public IEnumerable<ProductVariant> GetInStockVariants()
        => _variants.Where(v => !v.IsDeleted && v.IsActive && v.IsInStock);

    public IEnumerable<ProductVariant> GetLowStockVariants()
        => _variants.Where(v => !v.IsDeleted && v.IsLowStock);

    public IEnumerable<ProductVariant> GetOutOfStockVariants()
        => _variants.Where(v => !v.IsDeleted && !v.IsUnlimited && v.IsOutOfStock);

    public int GetActiveVariantsCount()
        => _variants.Count(v => !v.IsDeleted && v.IsActive);

    public ProductVariant? GetCheapestVariant()
        => _variants.Where(v => !v.IsDeleted && v.IsActive).OrderBy(v => v.SellingPrice).FirstOrDefault();

    public ProductVariant? GetMostExpensiveVariant()
        => _variants.Where(v => !v.IsDeleted && v.IsActive).OrderByDescending(v => v.SellingPrice).FirstOrDefault();

    public decimal GetMaxDiscountPercentage()
    {
        var activeVariants = _variants.Where(v => !v.IsDeleted && v.IsActive && v.HasDiscount);
        return activeVariants.Any() ? activeVariants.Max(v => v.DiscountPercentage) : 0;
    }

    #endregion Query Methods

    #region Aggregate Recalculation

    public void RecalculateAggregates()
    {
        var activeVariants = _variants.Where(v => !v.IsDeleted).ToList();

        if (activeVariants.Any())
        {
            MinPrice = Money.FromDecimal(activeVariants.Min(v => v.SellingPrice));
            MaxPrice = Money.FromDecimal(activeVariants.Max(v => v.SellingPrice));
            TotalStock = activeVariants.Where(v => !v.IsUnlimited).Sum(v => v.AvailableStock);
        }
        else
        {
            MinPrice = Money.Zero();
            MaxPrice = Money.Zero();
            TotalStock = 0;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateRating()
    {
        var approvedReviews = _reviews
            .Where(r => !r.IsDeleted && r.Status == ProductReview.ReviewStatus.Approved)
            .ToList();

        ReviewCount = approvedReviews.Count;
        AverageRating = ReviewCount > 0
            ? Math.Round((decimal)approvedReviews.Average(r => r.Rating), 1)
            : 0;
    }

    #endregion Aggregate Recalculation

    #region Domain Invariants

    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("محصول حذف شده است.");
    }

    private void EnsureCanAddMoreVariants()
    {
        if (_variants.Count(v => !v.IsDeleted) >= MaxVariantsPerProduct)
            throw new DomainException($"محصول نمی‌تواند بیش از {MaxVariantsPerProduct} واریانت داشته باشد.");
    }

    private void EnsureMinimumVariantsAfterRemoval()
    {
        if (_variants.Count(v => !v.IsDeleted) <= 1)
            throw new DomainException("محصول باید حداقل یک واریانت فعال داشته باشد.");
    }

    private void EnsureVariantSkuUnique(string normalizedSku, int? excludeVariantId)
    {
        var isDuplicate = _variants.Any(v =>
            !v.IsDeleted &&
            (excludeVariantId == null || v.Id != excludeVariantId) &&
            v.Sku == normalizedSku);

        if (isDuplicate)
            throw new DomainException($"SKU '{normalizedSku}' در این محصول تکراری است.");
    }

    private void EnsureVariantIsAvailableForPurchase(ProductVariant variant)
    {
        if (!variant.IsActive) throw new DomainException("این واریانت غیرفعال است و قابل خرید نیست.");
        if (variant.IsDeleted) throw new DomainException("این واریانت حذف شده است.");
        if (!IsActive) throw new DomainException("این محصول غیرفعال است و قابل خرید نیست.");
    }

    private static void EnsureVariantCanFulfill(ProductVariant variant, int quantity)
    {
        if (!variant.CanFulfill(quantity))
            throw new InsufficientStockException(variant.Id, variant.AvailableStock, quantity);
    }

    private ProductVariant GetVariantOrThrow(int variantId)
    {
        return _variants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted)
            ?? throw new DomainException("واریانت مورد نظر یافت نشد.");
    }

    private ProductReview GetReviewOrThrow(int reviewId)
    {
        return _reviews.FirstOrDefault(r => r.Id == reviewId && !r.IsDeleted)
            ?? throw new DomainException("نظر مورد نظر یافت نشد.");
    }

    private void ValidateAttributeCombination(List<AttributeValue> attributes)
    {
        if (!attributes.Any()) return;

        var typeIds = attributes.Select(a => a.AttributeTypeId).ToList();
        if (typeIds.Count != typeIds.Distinct().Count())
            throw new DomainException("هر واریانت نمی‌تواند چند مقدار برای یک نوع ویژگی داشته باشد.");

        var attributeSet = attributes.Select(a => a.Id).OrderBy(x => x).ToList();
        var duplicateVariant = _variants.FirstOrDefault(v =>
            !v.IsDeleted &&
            v.VariantAttributes.Select(va => va.AttributeValueId).OrderBy(x => x).SequenceEqual(attributeSet));

        if (duplicateVariant != null)
            throw new DomainException("ترکیب ویژگی‌های انتخابی تکراری است.");
    }

    private void CheckAndRaiseStockEvents(ProductVariant variant)
    {
        if (variant.IsUnlimited) return;

        if (variant.IsOutOfStock)
            AddDomainEvent(new OutOfStockEvent(variant.Id, Id, Name));
        else if (variant.IsLowStock)
            AddDomainEvent(new LowStockWarningEvent(variant.Id, Id, Name, variant.AvailableStock, variant.LowStockThreshold));
    }

    private static void ValidateName(string name)
    {
        if (name.Length < 2) throw new DomainException("نام محصول باید حداقل ۲ کاراکتر باشد.");
        if (name.Length > 200) throw new DomainException("نام محصول نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");
    }

    private static void ValidateSku(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return;
        if (sku.Length > 50) throw new DomainException("SKU نمی‌تواند بیش از ۵۰ کاراکتر باشد.");
        var normalized = sku.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z0-9\-_]+$"))
            throw new DomainException("SKU فقط می‌تواند شامل حروف، اعداد، خط تیره و زیرخط باشد.");
    }

    private static string? NormalizeSku(string? sku)
    {
        return string.IsNullOrWhiteSpace(sku) ? null : sku.Trim().ToUpperInvariant();
    }

    #endregion Domain Invariants
}