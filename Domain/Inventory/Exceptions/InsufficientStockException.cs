namespace Domain.Inventory.Exceptions;

public sealed class InsufficientStockException(ProductVariantId variantId, int requested, int available) : Exception($"Insufficient stock for variant '{variantId}'. Requested: {requested}, Available: {available}.")
{
    public ProductVariantId VariantId { get; } = variantId;
    public int Requested { get; } = requested;
    public int Available { get; } = available;
}