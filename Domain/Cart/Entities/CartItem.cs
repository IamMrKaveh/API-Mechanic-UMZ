namespace Domain.Cart.Entities;

public sealed class CartItem : Entity<Guid>
{
    public Guid CartId { get; private init; }
    public Guid VariantId { get; private init; }
    public Guid ProductId { get; private init; }
    public string ProductName { get; private init; } = null!;
    public string Sku { get; private init; } = null!;
    public Money UnitPrice { get; private init; } = null!;
    public Money OriginalPrice { get; private init; } = null!;
    public int Quantity { get; private set; }
    public DateTime AddedAt { get; private init; }

    private CartItem()
    { }

    private CartItem(
        Guid id,
        Guid cartId,
        Guid variantId,
        Guid productId,
        string productName,
        string sku,
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
        Guid cartId,
        Guid variantId,
        Guid productId,
        string productName,
        string sku,
        Money unitPrice,
        Money originalPrice,
        int quantity)
    {
        if (quantity <= 0)
            throw new InvalidCartQuantityException(quantity);

        return new CartItem(
            Guid.NewGuid(),
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