using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Order.Entities;

public sealed class OrderItem : Entity<OrderItemId>
{
    public string ProductName { get; private init; } = null!;
    public string Sku { get; private init; } = null!;
    public Money UnitPrice { get; private init; } = null!;
    public int Quantity { get; private init; }

    public Order.Aggregates.Order Order { get; private init; } = default!;
    public OrderId OrderId { get; private init; } = default!;
    public Variant.Aggregates.ProductVariant Variant { get; private init; } = default!;
    public VariantId VariantId { get; private init; } = default!;
    public Product.Aggregates.Product Product { get; private init; } = default!;
    public ProductId ProductId { get; private init; } = default!;

    private OrderItem()
    { }

    private OrderItem(
        OrderItemId id,
        OrderId orderId,
        VariantId variantId,
        ProductId productId,
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

    internal static OrderItem FromSnapshot(OrderId orderId, OrderItemSnapshot snapshot)
    {
        return new OrderItem(
            OrderItemId.NewId(),
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