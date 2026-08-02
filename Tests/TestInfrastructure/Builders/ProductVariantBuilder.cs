using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class ProductVariantBuilder
{
    private VariantId _id = VariantId.NewId();
    private ProductId _productId = ProductId.NewId();
    private Sku _sku = new SkuBuilder().Build();
    private Money _sellingPrice = Money.Create(100_000m, "IRT");
    private Money? _originalPrice;

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

    public ProductVariantBuilder WithSku(Sku sku)
    {
        _sku = sku;
        return this;
    }

    public ProductVariantBuilder WithSku(string value)
    {
        _sku = Sku.Create(value);
        return this;
    }

    public ProductVariantBuilder WithSellingPrice(Money price)
    {
        _sellingPrice = price;
        return this;
    }

    public ProductVariantBuilder WithSellingPrice(decimal amount, string currency = "IRT")
    {
        _sellingPrice = Money.Create(amount, currency);
        return this;
    }

    public ProductVariantBuilder WithOriginalPrice(Money? price)
    {
        _originalPrice = price;
        return this;
    }

    public ProductVariantBuilder WithOriginalPrice(decimal amount, string currency = "IRT")
    {
        _originalPrice = Money.Create(amount, currency);
        return this;
    }

    public ProductVariant Build() =>
        ProductVariant.Create(_id, _productId, _sku, _sellingPrice, _originalPrice);
}
