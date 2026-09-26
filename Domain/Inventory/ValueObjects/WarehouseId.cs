namespace Domain.Inventory.ValueObjects;

public sealed record WarehouseId : StronglyTypedId<WarehouseId>
{
    private WarehouseId(Guid value) : base(value) { }

    public static implicit operator Guid(WarehouseId id) => id.Value;

    public override string ToString() => Value.ToString();
}
