namespace Domain.Inventory.ValueObjects;

public sealed record InventoryId : StronglyTypedId<InventoryId>
{
    private InventoryId(Guid value) : base(value) { }

    public static implicit operator Guid(InventoryId id) => id.Value;

    public override string ToString() => Value.ToString();
}
