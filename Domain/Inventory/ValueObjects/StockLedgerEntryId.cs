namespace Domain.Inventory.ValueObjects;

public sealed record StockLedgerEntryId : IStronglyTypedId
{
    public Guid Value { get; }

    private StockLedgerEntryId(Guid value) => Value = value;

    public static StockLedgerEntryId NewId() => new(Guid.NewGuid());

    public static StockLedgerEntryId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("StockLedgerEntryId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(StockLedgerEntryId id) => id.Value;
}