namespace Domain.Inventory.ValueObjects;

public sealed record InventoryId : IStronglyTypedId
{
    public Guid Value { get; }

    private InventoryId(Guid value) => Value = value;

    public static InventoryId NewId() => new(Guid.NewGuid());

    public static InventoryId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("InventoryId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(InventoryId id) => id.Value;
}