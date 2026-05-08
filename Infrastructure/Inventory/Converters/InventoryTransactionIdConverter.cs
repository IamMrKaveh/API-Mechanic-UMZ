using Domain.Inventory.ValueObjects;

namespace Infrastructure.Inventory.Converters;

internal sealed class InventoryTransactionIdConverter : StronglyTypedIdConverter<InventoryTransactionId>
{
    public InventoryTransactionIdConverter() : base(InventoryTransactionId.From)
    {
    }
}