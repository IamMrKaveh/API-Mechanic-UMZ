using Domain.Cart.Exceptions;
using Domain.Cart.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Cart.Entities;

public sealed class CartItem : Entity<CartItemId>
{
    public CartId CartId { get; private init; } = default!;
    public ProductVariantId VariantId { get; private init; } = default!;
    public ProductId ProductId { get; private init; } = default!;
    public ProductName ProductName { get; private init; } = null!;
    public Sku Sku { get; private init; } = null!;
    public Money UnitPrice { get; private init; } = null!;
    public Money OriginalPrice { get; private init; } = null!;
    public int Quantity { get; private set; }
    public DateTime AddedAt { get; private init; }

    private CartItem()
    { }

    private CartItem(
        CartItemId id,
        CartId cartId,
        ProductVariantId variantId,
        ProductId productId,
        ProductName productName,
        Sku sku,
        Money unitPrice,
        Money originalPrice,
        int quantity) : base(id)
    {
        CartId = cartId;
        VariantId = variantId;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        UnitPrice = unitPrice;
        OriginalPrice = originalPrice;
        Quantity = quantity;
        AddedAt = DateTime.UtcNow;
    }

    internal static CartItem Create(
        CartId cartId,
        ProductVariantId variantId,
        ProductId productId,
        ProductName productName,
        Sku sku,
        Money unitPrice,
        Money originalPrice,
        int quantity)
    {
        if (quantity <= 0)
            throw new InvalidCartQuantityException(quantity);

        return new CartItem(
            CartItemId.NewId(),
            cartId,
            variantId,
            productId,
            productName,
            sku,
            unitPrice,
            originalPrice,
            quantity);
    }

    internal void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new InvalidCartQuantityException(newQuantity);
        Quantity = newQuantity;
    }

    internal void IncrementQuantity(int additionalQuantity)
    {
        if (additionalQuantity <= 0)
            throw new InvalidCartQuantityException(additionalQuantity);
        Quantity += additionalQuantity;
    }

    public Money TotalPrice => UnitPrice.Multiply(Quantity);
}