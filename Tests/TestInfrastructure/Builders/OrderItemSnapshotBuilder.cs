using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class OrderItemSnapshotBuilder
{
    private VariantId _variantId = VariantId.NewId();
    private ProductId _productId = ProductId.NewId();
    private ProductName _productName = ProductName.Create("Product X");
    private Sku _sku = Sku.Create("SKU-001");
    private Money _unitPrice = Money.Create(100m, "IRT");
    private int _quantity = 1;

    public OrderItemSnapshotBuilder WithVariantId(VariantId variantId)
    {
        _variantId = variantId;
        return this;
    }

    public OrderItemSnapshotBuilder WithProductId(ProductId productId)
    {
        _productId = productId;
        return this;
    }

    public OrderItemSnapshotBuilder WithProductName(ProductName productName)
    {
        _productName = productName;
        return this;
    }

    public OrderItemSnapshotBuilder WithSku(Sku sku)
    {
        _sku = sku;
        return this;
    }

    public OrderItemSnapshotBuilder WithUnitPrice(Money unitPrice)
    {
        _unitPrice = unitPrice;
        return this;
    }

    public OrderItemSnapshotBuilder WithUnitPrice(decimal amount, string currency = "IRT")
    {
        _unitPrice = Money.Create(amount, currency);
        return this;
    }

    public OrderItemSnapshotBuilder WithQuantity(int quantity)
    {
        _quantity = quantity;
        return this;
    }

    public OrderItemSnapshot Build() =>
        OrderItemSnapshot.Create(_variantId, _productId, _productName, _sku, _unitPrice, _quantity);
}
