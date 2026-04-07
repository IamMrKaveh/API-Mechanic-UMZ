using Domain.Cart.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Cart.Events;

public sealed class CartItemAddedEvent(
    CartId cartId,
    ProductVariantId variantId,
    ProductId productId,
    ProductName productName,
    int quantity,
    decimal unitPrice) : DomainEvent
{
    public CartId CartId { get; } = cartId;
    public ProductVariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public ProductName ProductName { get; } = productName;
    public int Quantity { get; } = quantity;
    public decimal UnitPrice { get; } = unitPrice;
}