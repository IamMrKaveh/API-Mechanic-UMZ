using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Exceptions;

public sealed class InsufficientStockException(VariantId variantId, int requested, int available) : Exception($"Insufficient stock for variant '{variantId}'. Requested: {requested}, Available: {available}.")
{
    public VariantId VariantId { get; } = variantId;
    public int Requested { get; } = requested;
    public int Available { get; } = available;
}