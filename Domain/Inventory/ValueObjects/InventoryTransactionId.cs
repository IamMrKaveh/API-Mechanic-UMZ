namespace Domain.Inventory.ValueObjects;

public sealed record InventoryTransactionId : IStronglyTypedId
{
    public Guid Value { get; }

    private InventoryTransactionId(Guid value) => Value = value;

    public static InventoryTransactionId NewId() => new(Guid.NewGuid());

    public static InventoryTransactionId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("InventoryTransactionId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(InventoryTransactionId id) => id.Value;
}