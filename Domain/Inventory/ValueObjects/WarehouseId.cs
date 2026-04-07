using System;

namespace Domain.Inventory.ValueObjects;

public sealed record WarehouseId
{
    public Guid Value { get; }

    private WarehouseId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty.", nameof(value));

        Value = value;
    }

    public static WarehouseId NewId() => new(Guid.NewGuid());

    public static WarehouseId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}