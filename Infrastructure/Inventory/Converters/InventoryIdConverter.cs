using Domain.Inventory.ValueObjects;

namespace Infrastructure.Inventory.Converters;

internal sealed class InventoryIdConverter : StronglyTypedIdConverter<InventoryId>
{
    public InventoryIdConverter() : base(InventoryId.From)
    {
    }
}