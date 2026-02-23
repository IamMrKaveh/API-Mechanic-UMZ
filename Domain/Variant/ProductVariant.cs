namespace Domain.Variant;

public class ProductVariant : AggregateRoot, IAuditable, ISoftDeletable, IActivatable
{
    public int ProductId { get; private set; }
    public Sku Sku { get; private set; } = null!;
    public Money PurchasePrice { get; private set; } = null!;
    public Money SellingPrice { get; private set; } = null!;
    public Money OriginalPrice { get; private set; } = null!;
    public int StockQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public bool IsUnlimited { get; private set; }
    public int LowStockThreshold { get; private set; }
    public bool IsActive { get; private set; }
    public decimal ShippingMultiplier { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    public Product.Product Product { get; private set; } = null!;
    public ICollection<ProductVariantAttribute> VariantAttributes { get; private set; } = new List<ProductVariantAttribute>();
    public ICollection<ProductVariantShipping> ProductVariantShippings { get; private set; } = new List<ProductVariantShipping>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; private set; } = new List<InventoryTransaction>();
    public ICollection<CartItem> CartItems { get; private set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();

    [NotMapped]
    public int AvailableStock => IsUnlimited ? int.MaxValue : Math.Max(0, StockQuantity - ReservedQuantity);

    [NotMapped]
    public bool IsInStock => IsUnlimited || AvailableStock > 0;

    [NotMapped]
    public bool HasDiscount => OriginalPrice.Amount > SellingPrice.Amount;

    [NotMapped]
    public decimal DiscountPercentage =>
        HasDiscount
            ? Math.Round((OriginalPrice.Amount - SellingPrice.Amount) / OriginalPrice.Amount * 100, 2)
            : 0;

    [NotMapped]
    public string DisplayName
    {
        get
        {
            var attrs = VariantAttributes
                .Where(x => x.AttributeValue != null)
                .Select(x => x.AttributeValue.DisplayValue)
                .ToList();
            return attrs.Any() ? string.Join(" / ", attrs) : (Sku?.Value ?? "Default");
        }
    }

    [NotMapped]
    public bool IsLowStock => !IsUnlimited && AvailableStock > 0 && AvailableStock <= LowStockThreshold;

    [NotMapped]
    public bool IsOutOfStock => !IsUnlimited && AvailableStock <= 0;

    public bool CanFulfill(int quantity) => IsUnlimited || AvailableStock >= quantity;

    private ProductVariant()
    { }

    public static ProductVariant Create(
        int productId, string sku,
        Money purchasePrice, Money sellingPrice, Money originalPrice,
        int stockQuantity, bool isUnlimited, decimal shippingMultiplier,
        int lowStockThreshold = 5)
    {
        var variant = new ProductVariant
        {
            ProductId = productId,
            Sku = Sku.Create(sku),
            PurchasePrice = purchasePrice,
            SellingPrice = sellingPrice,
            OriginalPrice = originalPrice,
            StockQuantity = stockQuantity,
            IsUnlimited = isUnlimited,
            ShippingMultiplier = shippingMultiplier,
            LowStockThreshold = lowStockThreshold,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ReservedQuantity = 0
        };

        variant.AddDomainEvent(new ProductVariantCreatedEvent(variant.Id, productId));
        return variant;
    }

    public void UpdateDetails(string? sku, decimal shippingMultiplier, int? lowStockThreshold = null)
    {
        if (!string.IsNullOrWhiteSpace(sku))
            Sku = Sku.Create(sku);

        if (shippingMultiplier <= 0)
            throw new DomainException("ضریب ارسال باید بزرگتر از صفر باشد.");

        ShippingMultiplier = shippingMultiplier;
        if (lowStockThreshold.HasValue) LowStockThreshold = lowStockThreshold.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPricing(decimal purchasePrice, decimal sellingPrice, decimal originalPrice)
    {
        if (sellingPrice < purchasePrice)
            throw new DomainException("قیمت فروش نمی‌تواند کمتر از قیمت خرید باشد.");

        if (originalPrice > 0 && sellingPrice > originalPrice)
            throw new DomainException("قیمت فروش نمی‌تواند بیشتر از قیمت اصلی (خط خورده) باشد.");

        var oldPrice = SellingPrice?.Amount ?? 0;
        PurchasePrice = Money.FromDecimal(purchasePrice);
        SellingPrice = Money.FromDecimal(sellingPrice);
        OriginalPrice = Money.FromDecimal(originalPrice);
        UpdatedAt = DateTime.UtcNow;

        if (SellingPrice.Amount != oldPrice)
            AddDomainEvent(new VariantPriceChangedEvent(Id, ProductId, SellingPrice.Amount));
    }

    /// <summary>
    /// رویداد self-contained با مقادیر جدید (بدون نیاز به DB query در handler)
    /// </summary>
    public void AdjustStock(int quantity)
    {
        if (!IsUnlimited && StockQuantity + quantity < 0)
            throw new DomainException($"موجودی نمی‌تواند منفی شود. موجودی فعلی: {StockQuantity}");

        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;

        // FIX #10: payload کامل با مقادیر جدید
        AddDomainEvent(new VariantStockChangedEvent(
            Id, ProductId, quantity,
            newOnHand: StockQuantity,
            newReserved: ReservedQuantity,
            newAvailable: AvailableStock,
            isInStock: IsInStock));
    }

    public void AddStock(int quantity)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        AdjustStock(quantity);
    }

    public void Reserve(int quantity)
    {
        if (IsUnlimited) return;
        if (AvailableStock < quantity)
            throw new DomainException("موجودی کافی برای رزرو وجود ندارد.");
        ReservedQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release(int quantity)
    {
        if (IsUnlimited) return;
        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
        UpdatedAt = DateTime.UtcNow;

        // emit رویداد با مقادیر جدید پس از release
        AddDomainEvent(new VariantStockChangedEvent(
            Id, ProductId, quantity,
            newOnHand: StockQuantity,
            newReserved: ReservedQuantity,
            newAvailable: AvailableStock,
            isInStock: IsInStock));
    }

    /// <summary>
    /// Commit رزرو - کاهش هر دو Reserved و OnHand
    /// </summary>
    public void ConfirmReservation(int quantity)
    {
        if (IsUnlimited) return;
        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
        StockQuantity = Math.Max(0, StockQuantity - quantity);
        UpdatedAt = DateTime.UtcNow;

        // FIX #10: payload کامل
        AddDomainEvent(new VariantStockChangedEvent(
            Id, ProductId, -quantity,
            newOnHand: StockQuantity,
            newReserved: ReservedQuantity,
            newAvailable: AvailableStock,
            isInStock: IsInStock));
    }

    public void SetUnlimited(bool isUnlimited)
    {
        if (IsUnlimited == isUnlimited) return;

        IsUnlimited = isUnlimited;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new VariantUnlimitedChangedEvent(Id, ProductId, isUnlimited));
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignAttributes(List<AttributeValue> newAttributes)
    {
        var newIds = newAttributes.Select(a => a.Id).ToHashSet();
        var currentAttributes = VariantAttributes.ToList();

        var toRemove = currentAttributes.Where(x => !newIds.Contains(x.AttributeValueId)).ToList();
        foreach (var item in toRemove)
            VariantAttributes.Remove(item);

        var existingIds = currentAttributes.Select(x => x.AttributeValueId).ToHashSet();
        foreach (var attr in newAttributes)
        {
            if (!existingIds.Contains(attr.Id))
            {
                VariantAttributes.Add(new ProductVariantAttribute
                {
                    AttributeValueId = attr.Id,
                    AttributeValue = attr
                });
            }
        }
    }

    public void SoftDelete(int deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddShipping(Shipping.Shipping sm)
    {
        if (!ProductVariantShippings.Any(x => x.ShippingId == sm.Id))
            ProductVariantShippings.Add(new ProductVariantShipping { ShippingId = sm.Id, ProductVariantId = Id, Shipping = sm });
    }

    public void RemoveShipping(int shippingId)
    {
        var sm = ProductVariantShippings.FirstOrDefault(x => x.ShippingId == shippingId);
        if (sm != null) ProductVariantShippings.Remove(sm);
    }
}