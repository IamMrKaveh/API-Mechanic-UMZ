namespace Domain.Inventory.ValueObjects;

public sealed record StockLedgerEntryId(Guid Value)
{
    public static StockLedgerEntryId NewId() => new(Guid.NewGuid());
    public static StockLedgerEntryId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}