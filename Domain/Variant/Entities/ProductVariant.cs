using Domain.Common.Base;
using Domain.Common.Gaurd;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Attribute.Entities;
using Domain.Order;

namespace Domain.Variant.Entities;

public class ProductVariant : BaseEntity, ISoftDeletable, IAuditable, IActivatable
{
    // ============================================================
    // State
    // ============================================================
    public int ProductId { get; private set; }

    public Sku? Sku { get; private set; }
    public decimal PurchasePrice { get; private set; }
    public decimal OriginalPrice { get; private set; }
    public decimal SellingPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public bool IsUnlimited { get; private set; }
    public decimal ShippingMultiplier { get; private set; } = 1m;
    public bool IsActive { get; private set; } = true;
    public int LowStockThreshold { get; private set; } = 5;

    // Audit & Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public new byte[]? RowVersion { get; private set; }

    // Navigations
    public Domain.Product.Entities.Product? Product { get; private set; }

    // Join Entities for Many-to-Many
    private readonly List<ProductVariantAttribute> _variantAttributes = new();

    public IReadOnlyCollection<ProductVariantAttribute> VariantAttributes => _variantAttributes.AsReadOnly();

    private readonly List<ProductVariantShippingMethod> _shippingMethods = new();
    public IReadOnlyCollection<ProductVariantShippingMethod> ProductVariantShippingMethods => _shippingMethods.AsReadOnly();

    // Other Navigations
    public ICollection<Media.Media> Images { get; private set; } = new List<Media.Media>();

    public ICollection<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();
    public ICollection<Cart.CartItem> CartItems { get; private set; } = new List<Cart.CartItem>();
    public ICollection<Inventory.InventoryTransaction> InventoryTransactions { get; private set; } = new List<Inventory.InventoryTransaction>();

    // Computed Properties
    public int AvailableStock => IsUnlimited ? int.MaxValue : Math.Max(0, StockQuantity - ReservedQuantity);

    public bool IsInStock => IsUnlimited || AvailableStock > 0;
    public bool IsLowStock => !IsUnlimited && AvailableStock <= LowStockThreshold && AvailableStock > 0;
    public bool IsOutOfStock => !IsUnlimited && AvailableStock <= 0;
    public bool HasDiscount => OriginalPrice > SellingPrice;

    public decimal DiscountPercentage => HasDiscount && OriginalPrice > 0
        ? Math.Round((1 - (SellingPrice / OriginalPrice)) * 100, 2)
        : 0;

    public string DisplayName => !string.IsNullOrEmpty(GetAttributesSummary())
        ? GetAttributesSummary()
        : Sku?.Value ?? $"#{Id}";

    private ProductVariant()
    { }

    // ============================================================
    // Factory (Internal - called by Product Aggregate Root)
    // ============================================================
    internal static ProductVariant Create(
        Domain.Product.Entities.Product product, string? sku,
        decimal purchasePrice, decimal sellingPrice, decimal originalPrice,
        int stock, bool isUnlimited, decimal shippingMultiplier)
    {
        Guard.Against.Null(product, nameof(product));
        ValidatePricing(purchasePrice, sellingPrice, originalPrice);
        ValidateStock(stock, isUnlimited);

        return new ProductVariant
        {
            ProductId = product.Id,
            Product = product,
            Sku = Sku.CreateOptional(sku),
            PurchasePrice = purchasePrice,
            SellingPrice = sellingPrice,
            OriginalPrice = originalPrice,
            StockQuantity = stock,
            IsUnlimited = isUnlimited,
            ShippingMultiplier = shippingMultiplier,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // ============================================================
    // Details Management
    // ============================================================
    internal void UpdateDetails(string? sku, decimal shippingMultiplier)
    {
        EnsureNotDeleted();
        Sku = Sku.CreateOptional(sku);
        if (shippingMultiplier < 0) throw new DomainException("ضریب حمل و نقل نمی‌تواند منفی باشد.");
        ShippingMultiplier = shippingMultiplier;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void Activate()
    {
        EnsureNotDeleted();
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void Delete(int? deletedBy)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }

    internal void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // ============================================================
    // Pricing & Inventory Logic (Internal)
    // ============================================================
    internal void SetPricing(decimal purchasePrice, decimal sellingPrice, decimal originalPrice)
    {
        EnsureNotDeleted();
        ValidatePricing(purchasePrice, sellingPrice, originalPrice);

        PurchasePrice = purchasePrice;
        SellingPrice = sellingPrice;
        OriginalPrice = originalPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void AdjustStock(int quantityChange)
    {
        EnsureNotDeleted();
        if (IsUnlimited) return;

        var newStock = StockQuantity + quantityChange;
        if (newStock < 0) throw new DomainException($"موجودی منفی مجاز نیست. موجودی فعلی: {StockQuantity}");

        StockQuantity = newStock;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reserve(int quantity)
    {
        EnsureNotDeleted();
        if (IsUnlimited) return;
        if (AvailableStock < quantity) throw new DomainException($"موجودی کافی نیست.");

        ReservedQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release(int quantity)
    {
        EnsureNotDeleted();
        if (IsUnlimited) return;
        var releaseAmount = Math.Min(quantity, ReservedQuantity);
        ReservedQuantity -= releaseAmount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmReservation(int quantity)
    {
        EnsureNotDeleted();
        if (IsUnlimited) return;
        if (ReservedQuantity < quantity) throw new DomainException("تعداد تایید شده بیشتر از رزرو است.");

        ReservedQuantity -= quantity;
        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void SetUnlimited(bool isUnlimited)
    {
        IsUnlimited = isUnlimited;
        UpdatedAt = DateTime.UtcNow;
    }

    // ============================================================
    // Attribute Management (Internal)
    // ============================================================
    internal void AddAttribute(AttributeValue attributeValue)
    {
        EnsureNotDeleted();
        if (_variantAttributes.Any(va => va.AttributeValueId == attributeValue.Id)) return;

        _variantAttributes.Add(new ProductVariantAttribute
        {
            Variant = this,
            VariantId = this.Id,
            AttributeValueId = attributeValue.Id,
            AttributeValue = attributeValue
        });
        UpdatedAt = DateTime.UtcNow;
    }

    internal void RemoveAttribute(int attributeValueId)
    {
        EnsureNotDeleted();
        var attr = _variantAttributes.FirstOrDefault(va => va.AttributeValueId == attributeValueId);
        if (attr != null)
        {
            _variantAttributes.Remove(attr);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    internal void ClearAttributes()
    {
        _variantAttributes.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public string GetAttributesSummary()
    {
        if (!_variantAttributes.Any()) return string.Empty;
        return string.Join(" - ", _variantAttributes
            .OrderBy(va => va.AttributeValue?.AttributeType?.SortOrder ?? 0)
            .Select(va => va.AttributeValue?.DisplayValue ?? ""));
    }

    // ============================================================
    // Shipping Methods Management (Internal)
    // ============================================================
    internal void AddShippingMethod(ShippingMethod shippingMethod)
    {
        EnsureNotDeleted();
        if (_shippingMethods.Any(sm => sm.ShippingMethodId == shippingMethod.Id)) return;

        _shippingMethods.Add(new ProductVariantShippingMethod
        {
            ProductVariant = this,
            ProductVariantId = this.Id,
            ShippingMethodId = shippingMethod.Id,
            ShippingMethod = shippingMethod,
            IsActive = true
        });
        UpdatedAt = DateTime.UtcNow;
    }

    internal void RemoveShippingMethod(int shippingMethodId)
    {
        var item = _shippingMethods.FirstOrDefault(sm => sm.ShippingMethodId == shippingMethodId);
        if (item != null)
        {
            _shippingMethods.Remove(item);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    internal void SetVariantShippingMethods(List<ShippingMethod> methods)
    {
        _shippingMethods.RemoveAll(x => !methods.Any(m => m.Id == x.ShippingMethodId));
        foreach (var method in methods) AddShippingMethod(method);
    }

    // ============================================================
    // Private Helpers
    // ============================================================
    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("واریانت حذف شده است.");
    }

    private static void ValidatePricing(decimal purchase, decimal selling, decimal original)
    {
        if (purchase < 0 || selling < 0 || original < 0)
            throw new DomainException("قیمت‌ها نمی‌توانند منفی باشند.");

        if (selling < purchase)
            throw new DomainException("قیمت فروش کمتر از خرید مجاز نیست.");

        if (original > 0 && selling > original)
            throw new DomainException("قیمت فروش بیشتر از قیمت اصلی مجاز نیست.");
    }

    private static void ValidateStock(int stock, bool isUnlimited)
    {
        if (!isUnlimited && stock < 0) throw new DomainException("موجودی اولیه نامعتبر است.");
    }
}