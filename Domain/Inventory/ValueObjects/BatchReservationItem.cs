using Domain.Variant.ValueObjects;

namespace Domain.Inventory.ValueObjects;

public sealed record BatchReservationItem(VariantId VariantId, StockQuantity Quantity)
{
    public static BatchReservationItem Create(VariantId variantId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(variantId);
        var stockQuantity = StockQuantity.Create(quantity);
        return new BatchReservationItem(variantId, stockQuantity);
    }
}