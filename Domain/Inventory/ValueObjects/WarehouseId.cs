namespace Domain.Inventory.ValueObjects;

public sealed record WarehouseId(Guid Value)
{
    public static WarehouseId NewId() => new(Guid.NewGuid());
    public static WarehouseId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}