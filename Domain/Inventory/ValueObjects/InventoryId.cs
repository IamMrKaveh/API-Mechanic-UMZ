using System;

namespace Domain.Inventory.ValueObjects;

public sealed record InventoryId
{
    public Guid Value { get; }

    private InventoryId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("InventoryId cannot be empty.", nameof(value));

        Value = value;
    }

    public static InventoryId NewId() => new(Guid.NewGuid());

    public static InventoryId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}