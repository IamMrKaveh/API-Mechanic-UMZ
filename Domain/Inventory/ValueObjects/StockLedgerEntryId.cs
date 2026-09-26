namespace Domain.Inventory.ValueObjects;

public sealed record StockLedgerEntryId : StronglyTypedId<StockLedgerEntryId>
{
    private StockLedgerEntryId(Guid value) : base(value) { }

    public static implicit operator Guid(StockLedgerEntryId id) => id.Value;

    public override string ToString() => Value.ToString();
}
