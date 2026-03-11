namespace Domain.Cart.Exceptions;

public sealed class CartNotFoundException(Guid cartId) : Exception($"Cart with ID '{cartId}' was not found.")
{
}

public sealed class CartItemNotFoundException(Guid variantId) : Exception($"Cart item for variant '{variantId}' was not found.")
{
}

public sealed class CartAlreadyCheckedOutException(Guid cartId) : Exception($"Cart '{cartId}' has already been checked out.")
{
}

public sealed class InvalidCartQuantityException(int quantity) : Exception($"Cart item quantity '{quantity}' is invalid. Must be greater than zero.")
{
}

public sealed class InsufficientStockForCartException(Guid variantId, int requestedQuantity, int availableStock) : DomainException($"Insufficient stock for variant '{variantId}'. Requested: {requestedQuantity}, Available: {availableStock}.")
{
    public Guid VariantId { get; } = variantId;
    public int RequestedQuantity { get; } = requestedQuantity;
    public int AvailableStock { get; } = availableStock;
}