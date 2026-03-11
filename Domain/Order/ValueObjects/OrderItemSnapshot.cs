namespace Domain.Order.ValueObjects;

public sealed record OrderItemSnapshot
{
    public Guid VariantId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string Sku { get; init; } = null!;
    public Money UnitPrice { get; init; } = null!;
    public int Quantity { get; init; }

    private OrderItemSnapshot() { }

    public static OrderItemSnapshot Create(
        Guid variantId,
        Guid productId,
        string productName,
        string sku,
        Money unitPrice,
        int quantity)
    {
        if (variantId == Guid.Empty)
            throw new ArgumentException("Variant ID cannot be empty.", nameof(variantId));
        if (productId == Guid.Empty)
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
            ProductName = productName.Trim(),
            Sku = sku.Trim().ToUpperInvariant(),
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }
}