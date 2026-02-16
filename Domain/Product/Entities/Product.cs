using Domain.Attribute.Entities;
using Domain.Categories;
using Domain.Common.Base;
using Domain.Common.Exceptions;
using Domain.Common.Gaurd;
using Domain.Common.Interfaces;
using Domain.Product.Events;
using Domain.Product.ValueObjects;
using Domain.Variant.Entities;
using Domain.Variant.Events;

namespace Domain.Product.Entities;

public class Product : AggregateRoot, ISoftDeletable, IAuditable, IActivatable
{
    // ============================================================
    // State
    // ============================================================
    public ProductName Name { get; private set; } = null!;

    public string? Description { get; private set; }
    public Sku? Sku { get; private set; } // SKU اصلی محصول (اختیاری، معمولا برای نمایش)
    public int CategoryGroupId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsFeatured { get; private set; }

    // Value Objects
    public ProductStats Stats { get; private set; } = ProductStats.CreateEmpty();

    // Audit & Soft Delete
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }
    public new byte[]? RowVersion { get; private set; }

    // ============================================================
    // Collections & Navigations
    // ============================================================
    private readonly List<ProductVariant> _variants = new();

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public CategoryGroup? CategoryGroup { get; private set; }
    public ICollection<Media.Media> Images { get; private set; } = new List<Media.Media>();
    public ICollection<Review.ProductReview> Reviews { get; private set; } = new List<Review.ProductReview>();

    // Computed
    public bool HasMultipleVariants => _variants.Count(v => !v.IsDeleted) > 1;

    private Product()
    { }

    // ============================================================
    // Factory
    // ============================================================
    public static Product Create(string name, string? description, string? sku, int categoryGroupId)
    {
        Guard.Against.NegativeOrZero(categoryGroupId, nameof(categoryGroupId));

        var productName = ProductName.Create(name);
        var productSku = ValueObjects.Sku.CreateOptional(sku);

        var product = new Product
        {
            Name = productName,
            Description = description?.Trim(),
            Sku = productSku,
            CategoryGroupId = categoryGroupId,
            IsActive = true,
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow,
            Stats = ProductStats.CreateEmpty()
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, productName.Value, categoryGroupId));
        return product;
    }

    // ============================================================
    // Variant Management (Delegation to Variant Aggregate Part)
    // ============================================================
    public ProductVariant AddVariant(
        string? sku,
        decimal purchasePrice,
        decimal sellingPrice,
        decimal originalPrice,
        int stock,
        bool isUnlimited,
        decimal shippingMultiplier,
        List<AttributeValue> attributeValues)
    {
        EnsureNotDeleted();
        EnsureCanAddMoreVariants();

        if (!string.IsNullOrEmpty(sku))
        {
            var normalizedSku = ValueObjects.Sku.Create(sku).Value;
            if (_variants.Any(v => v.Sku?.Value == normalizedSku && !v.IsDeleted))
                throw new DomainException($"SKU '{normalizedSku}' در این محصول تکراری است.");
        }

        var variant = ProductVariant.Create(
            this, sku, purchasePrice, sellingPrice, originalPrice,
            stock, isUnlimited, shippingMultiplier);

        // افزودن ویژگی‌ها به واریانت
        foreach (var attrVal in attributeValues)
        {
            variant.AddAttribute(attrVal);
        }

        _variants.Add(variant);
        RefreshStats();

        AddDomainEvent(new ProductVariantAddedEvent(Id, variant.Id));
        return variant;
    }

    public void RemoveVariant(int variantId, int? deletedBy = null)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);

        if (IsActive && _variants.Count(v => !v.IsDeleted) <= 1)
            throw new DomainException("محصول فعال باید حداقل یک واریانت داشته باشد.");

        variant.Delete(deletedBy);
        RefreshStats();

        AddDomainEvent(new ProductVariantRemovedEvent(Id, variantId));
    }

    public void UpdateVariantDetails(int variantId, string? sku, decimal shippingMultiplier)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.UpdateDetails(sku, shippingMultiplier);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeVariantPrices(int variantId, decimal purchase, decimal selling, decimal original)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.SetPricing(purchase, selling, original);
        RefreshStats();
    }

    public void IncreaseStock(int variantId, int quantity)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.AdjustStock(quantity);
        RefreshStats();
    }

    public void DecreaseStock(int variantId, int quantity)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.AdjustStock(-quantity);
        RefreshStats();
    }

    public void SetVariantUnlimited(int variantId, bool isUnlimited)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.SetUnlimited(isUnlimited);
        RefreshStats();
    }

    public void SetVariantAttributes(int variantId, List<AttributeValue> attributeValues)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);

        variant.ClearAttributes();
        foreach (var attrVal in attributeValues)
        {
            variant.AddAttribute(attrVal);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddVariantShippingMethod(int variantId, Order.ShippingMethod method)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.AddShippingMethod(method);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVariantShippingMethods(int variantId, List<Order.ShippingMethod> methods)
    {
        EnsureNotDeleted();
        var variant = GetVariantOrThrow(variantId);
        variant.SetVariantShippingMethods(methods);
        UpdatedAt = DateTime.UtcNow;
    }

    public ProductVariant? FindVariant(int variantId)
        => _variants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted);

    // ============================================================
    // Product Lifecycle
    // ============================================================
    public void UpdateDetails(string name, string? description, string? sku, int categoryGroupId, bool isActive)
    {
        EnsureNotDeleted();
        Guard.Against.NegativeOrZero(categoryGroupId, nameof(categoryGroupId));

        var oldCategoryGroupId = CategoryGroupId;
        Name = ProductName.Create(name);
        Description = description?.Trim();
        Sku = ValueObjects.Sku.CreateOptional(sku);
        CategoryGroupId = categoryGroupId;

        if (isActive && !IsActive) Activate();
        else if (!isActive && IsActive) Deactivate();

        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductUpdatedEvent(Id, Name.Value));
        if (oldCategoryGroupId != categoryGroupId)
        {
            AddDomainEvent(new ProductCategoryChangedEvent(Id, oldCategoryGroupId, categoryGroupId));
        }
    }

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

    public void SetFeatured(bool isFeatured)
    {
        EnsureNotDeleted();
        IsFeatured = isFeatured;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(int? deletedBy)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;

        foreach (var variant in _variants.Where(v => !v.IsDeleted))
        {
            variant.Delete(deletedBy);
        }

        AddDomainEvent(new ProductDeletedEvent(Id, deletedBy));
    }

    public void MarkAsDeleted(int? deletedBy) => Delete(deletedBy);

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
        RefreshStats();
    }

    // ============================================================
    // Statistics
    // ============================================================
    public void RefreshStats()
    {
        var activeVariants = _variants.Where(v => !v.IsDeleted && v.IsActive).ToList();
        Stats = Stats.Recalculate(activeVariants);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateReviewStats(int count, decimal averageRating)
    {
        Stats = Stats.UpdateReviews(count, averageRating);
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementSalesCount(int quantity)
    {
        Stats = Stats.IncrementSales(quantity);
    }

    public Money CalculateSellingPrice(int variantId, DiscountCode? discountCode = null)
    {
        var variant = GetVariantOrThrow(variantId);
        var price = Money.FromDecimal(variant.SellingPrice);

        if (discountCode != null && discountCode.IsCurrentlyValid())
        {
            return discountCode.CalculateDiscountMoney(price);
        }
        return price;
    }

    // ============================================================
    // Helpers
    // ============================================================
    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("محصول حذف شده است.");
    }

    private void EnsureCanAddMoreVariants()
    {
        if (_variants.Count(v => !v.IsDeleted) >= 100)
            throw new DomainException("محدودیت تعداد واریانت.");
    }

    private ProductVariant GetVariantOrThrow(int variantId)
        => FindVariant(variantId) ?? throw new DomainException("واریانت یافت نشد.");
}