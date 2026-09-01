using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class ProductVariantBuilder
{
    private VariantId _id = VariantId.NewId();
    private ProductId _productId = ProductId.NewId();
    private Sku _sku = Sku.Create($"SKU-{Guid.NewGuid():N}"[..20]);
    private decimal _sellingAmount = 100_000m;
    private decimal? _originalAmount;
    private string _currency = "IRT";
    private bool _sellingPriceIsMoney;
    private Money? _sellingMoney;
    private Money? _originalMoney;

    public ProductVariantBuilder WithId(VariantId id)
    {
        _id = id;
        return this;
    }

    public ProductVariantBuilder WithProductId(ProductId productId)
    {
        _productId = productId;
        return this;
    }

    public ProductVariantBuilder WithSku(string sku)
    {
        _sku = Sku.Create(sku);
        return this;
    }

    public ProductVariantBuilder WithSku(Sku sku)
    {
        _sku = sku;
        return this;
    }

    public ProductVariantBuilder WithSellingPrice(decimal amount, string currency = "IRT")
    {
        _sellingAmount = amount;
        _currency = currency;
        _sellingPriceIsMoney = false;
        _sellingMoney = null;
        return this;
    }

    public ProductVariantBuilder WithSellingPrice(Money price)
    {
        _sellingMoney = price;
        _sellingPriceIsMoney = true;
        if (price is not null)
        {
            _sellingAmount = price.Amount;
            _currency = price.Currency;
        }
        return this;
    }

    public ProductVariantBuilder WithOriginalPrice(decimal amount, string currency = "IRT")
    {
        _originalAmount = amount;
        _currency = currency;
        _originalMoney = null;
        return this;
    }

    public ProductVariantBuilder WithOriginalPrice(Money? price)
    {
        _originalMoney = price;
        _originalAmount = price?.Amount;
        if (price is not null)
            _currency = price.Currency;
        return this;
    }

    public ProductVariant Build()
    {
        if (_sellingPriceIsMoney)
            return ProductVariant.Create(_id, _productId, _sku, _sellingMoney!, _originalMoney);

        return ProductVariant.Create(_id, _productId, _sku, _sellingAmount, _originalAmount, _currency);
    }
}
