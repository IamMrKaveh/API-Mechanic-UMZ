using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Order.ValueObjects;

public sealed record OrderItemSnapshot
{
    public VariantId VariantId { get; init; }
    public ProductId ProductId { get; init; }
    public ProductName ProductName { get; init; } = null!;
    public Sku Sku { get; init; } = null!;
    public Money UnitPrice { get; init; } = null!;
    public int Quantity { get; init; }

    private OrderItemSnapshot() { }

    public static OrderItemSnapshot Create(
        VariantId variantId,
        ProductId productId,
        ProductName productName,
        Sku sku,
        Money unitPrice,
        int quantity)
    {
        if (variantId is null)
            throw new ArgumentException("Variant ID cannot be empty.", nameof(variantId));
        if (productId is null)
            throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty.", nameof(productName));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty.", nameof(sku));
        ArgumentNullException.ThrowIfNull(unitPrice);
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        return new OrderItemSnapshot
        {
            VariantId = variantId,
            ProductId = productId,
            ProductName = productName,
            Sku = sku,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }
}