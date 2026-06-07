using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Order.ValueObjects;

public sealed record OrderItemSnapshot
{
    public VariantId VariantId { get; init; } = default!;
    public ProductId ProductId { get; init; } = default!;
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
            throw new DomainException("Variant ID cannot be empty.");
        if (productId is null)
            throw new DomainException("Product ID cannot be empty.");
        if (productName is null || string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name cannot be empty.");
        if (sku is null || string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU cannot be empty.");
        if (unitPrice is null)
            throw new DomainException("Unit price is required.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

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