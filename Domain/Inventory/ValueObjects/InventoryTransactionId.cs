namespace Domain.Inventory.ValueObjects;

public sealed record InventoryTransactionId(Guid Value)
{
    public static InventoryTransactionId NewId() => new(Guid.NewGuid());
    public static InventoryTransactionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}