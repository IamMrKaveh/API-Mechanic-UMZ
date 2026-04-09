namespace Domain.Inventory.ValueObjects;

public sealed record InventoryTransactionId
{
    public Guid Value { get; }

    private InventoryTransactionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("InventoryTransactionId cannot be empty.", nameof(value));

        Value = value;
    }

    public static InventoryTransactionId NewId() => new(Guid.NewGuid());

    public static InventoryTransactionId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}