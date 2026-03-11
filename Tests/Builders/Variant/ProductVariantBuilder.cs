using Domain.Common.ValueObjects;
using Domain.Variant.Aggregates;

namespace Tests.Builders.Variant;

public class ProductVariantBuilder
{
    private int _productId = 1;
    private string _sku = "SKU-TEST-001";
    private decimal _purchasePrice = 50_000;
    private decimal _sellingPrice = 100_000;
    private decimal _originalPrice = 120_000;
    private int _stockQuantity = 20;
    private bool _isUnlimited = false;
    private decimal _shippingMultiplier = 1.0m;
    private int _lowStockThreshold = 5;

    public ProductVariantBuilder WithProductId(int productId)
    {
        _productId = productId;
        return this;
    }

    public ProductVariantBuilder WithSku(string sku)
    {
        _sku = sku;
        return this;
    }

    public ProductVariantBuilder WithPurchasePrice(decimal purchasePrice)
    {
        _purchasePrice = purchasePrice;
        return this;
    }

    public ProductVariantBuilder WithSellingPrice(decimal sellingPrice)
    {
        _sellingPrice = sellingPrice;
        return this;
    }

    public ProductVariantBuilder WithOriginalPrice(decimal originalPrice)
    {
        _originalPrice = originalPrice;
        return this;
    }

    public ProductVariantBuilder WithStock(int stockQuantity)
    {
        _stockQuantity = stockQuantity;
        return this;
    }

    public ProductVariantBuilder WithUnlimited(bool isUnlimited)
    {
        _isUnlimited = isUnlimited;
        return this;
    }

    public ProductVariantBuilder WithShippingMultiplier(decimal multiplier)
    {
        _shippingMultiplier = multiplier;
        return this;
    }

    public ProductVariantBuilder WithLowStockThreshold(int threshold)
    {
        _lowStockThreshold = threshold;
        return this;
    }

    public ProductVariant Build()
    {
        return ProductVariant.Create(
            _productId,
            _sku,
            Money.FromDecimal(_purchasePrice),
            Money.FromDecimal(_sellingPrice),
            Money.FromDecimal(_originalPrice),
            _stockQuantity,
            _isUnlimited,
            _shippingMultiplier,
            _lowStockThreshold
        );
    }
}