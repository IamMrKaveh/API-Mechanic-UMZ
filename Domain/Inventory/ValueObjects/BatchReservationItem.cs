using Domain.Variant.ValueObjects;

namespace Domain.Inventory.ValueObjects;

public sealed record BatchReservationItem(VariantId VariantId, StockQuantity Quantity)
{
}