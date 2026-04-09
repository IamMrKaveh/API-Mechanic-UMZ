namespace Domain.Inventory.ValueObjects;

public sealed record StockLedgerEntryId
{
    public Guid Value { get; }

    private StockLedgerEntryId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("StockLedgerEntryId cannot be empty.", nameof(value));

        Value = value;
    }

    public static StockLedgerEntryId NewId() => new(Guid.NewGuid());

    public static StockLedgerEntryId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}