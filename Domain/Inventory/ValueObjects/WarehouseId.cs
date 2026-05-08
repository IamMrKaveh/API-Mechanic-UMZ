namespace Domain.Inventory.ValueObjects;

public sealed record WarehouseId : IStronglyTypedId
{
    public Guid Value { get; }

    private WarehouseId(Guid value) => Value = value;

    public static WarehouseId NewId() => new(Guid.NewGuid());

    public static WarehouseId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("WarehouseId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(WarehouseId id) => id.Value;
}