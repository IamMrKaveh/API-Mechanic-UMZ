using Domain.Cart.Aggregates;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class CartItemParametersBuilder
{
    private VariantId _variantId = VariantId.NewId();
    private ProductId _productId = ProductId.NewId();
    private ProductName _productName = ProductName.Create("Product X");
    private Sku _sku = Sku.Create("SKU-001");
    private Money _unitPrice = Money.Create(100m, "IRT");
    private Money _originalPrice = Money.Create(120m, "IRT");
    private int _quantity = 1;

    public CartItemParametersBuilder WithVariantId(VariantId variantId)
    {
        _variantId = variantId;
        return this;
    }

    public CartItemParametersBuilder WithProductId(ProductId productId)
    {
        _productId = productId;
        return this;
    }

    public CartItemParametersBuilder WithProductName(ProductName productName)
    {
        _productName = productName;
        return this;
    }

    public CartItemParametersBuilder WithProductName(string value)
    {
        _productName = ProductName.Create(value);
        return this;
    }

    public CartItemParametersBuilder WithSku(Sku sku)
    {
        _sku = sku;
        return this;
    }

    public CartItemParametersBuilder WithSku(string value)
    {
        _sku = Sku.Create(value);
        return this;
    }

    public CartItemParametersBuilder WithUnitPrice(Money price)
    {
        _unitPrice = price;
        return this;
    }

    public CartItemParametersBuilder WithUnitPrice(decimal amount, string currency = "IRT")
    {
        _unitPrice = Money.Create(amount, currency);
        return this;
    }

    public CartItemParametersBuilder WithOriginalPrice(Money price)
    {
        _originalPrice = price;
        return this;
    }

    public CartItemParametersBuilder WithOriginalPrice(decimal amount, string currency = "IRT")
    {
        _originalPrice = Money.Create(amount, currency);
        return this;
    }

    public CartItemParametersBuilder WithQuantity(int quantity)
    {
        _quantity = quantity;
        return this;
    }

    public void AddTo(Cart cart) =>
        cart.AddItem(_variantId, _productId, _productName, _sku, _unitPrice, _originalPrice, _quantity);

    public VariantId VariantId => _variantId;
    public ProductId ProductId => _productId;
    public ProductName ProductName => _productName;
    public Sku Sku => _sku;
    public Money UnitPrice => _unitPrice;
    public Money OriginalPrice => _originalPrice;
    public int Quantity => _quantity;
}
