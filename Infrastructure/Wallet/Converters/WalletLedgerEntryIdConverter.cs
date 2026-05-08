using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Converters;

internal sealed class WalletLedgerEntryIdConverter : StronglyTypedIdConverter<WalletLedgerEntryId>
{
    public WalletLedgerEntryIdConverter() : base(WalletLedgerEntryId.From)
    {
    }
}