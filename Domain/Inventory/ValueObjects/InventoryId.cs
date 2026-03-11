namespace Domain.Inventory.ValueObjects;

public sealed record InventoryId(Guid Value)
{
    public static InventoryId NewId() => new(Guid.NewGuid());
    public static InventoryId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}