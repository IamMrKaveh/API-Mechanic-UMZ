using Domain.Inventory.ValueObjects;

namespace Infrastructure.Inventory.Converters;

internal sealed class WarehouseIdConverter : StronglyTypedIdConverter<WarehouseId>
{
    public WarehouseIdConverter() : base(WarehouseId.From)
    {
    }
}