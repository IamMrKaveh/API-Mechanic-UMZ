using Domain.Inventory.ValueObjects;

namespace Infrastructure.Inventory.Converters;

internal sealed class StockLedgerEntryIdConverter : StronglyTypedIdConverter<StockLedgerEntryId>
{
    public StockLedgerEntryIdConverter() : base(StockLedgerEntryId.From)
    {
    }
}