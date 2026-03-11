namespace Domain.Order.Entities;

public sealed class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private init; }
    public Guid VariantId { get; private init; }
    public Guid ProductId { get; private init; }
    public string ProductName { get; private init; } = null!;
    public string Sku { get; private init; } = null!;
    public Money UnitPrice { get; private init; } = null!;
    public int Quantity { get; private init; }

    private OrderItem()
    { }

    private OrderItem(
        Guid id,
        Guid orderId,
        Guid variantId,
        Guid productId,
        string productName,
        string sku,
        Money unitPrice,
        int quantity) : base(id)
    {
        OrderId = orderId;
        VariantId = variantId;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    internal static OrderItem FromSnapshot(Guid orderId, OrderItemSnapshot snapshot)
    {
        return new OrderItem(
            Guid.NewGuid(),
            orderId,
            snapshot.VariantId,
            snapshot.ProductId,
            snapshot.ProductName,
            snapshot.Sku,
            snapshot.UnitPrice,
            snapshot.Quantity);
    }

    public Money TotalPrice => UnitPrice.Multiply(Quantity);
}