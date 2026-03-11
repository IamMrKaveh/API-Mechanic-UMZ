using Domain.Common.Events;

namespace Domain.Cart.Events;

public sealed class CartItemAddedEvent : DomainEvent
{
    public Guid CartId { get; }
    public Guid VariantId { get; }
    public Guid ProductId { get; }
    public string ProductName { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }

    public CartItemAddedEvent(
        Guid cartId,
        Guid variantId,
        Guid productId,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        CartId = cartId;
        VariantId = variantId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        EventVersion = 1;
    }
}