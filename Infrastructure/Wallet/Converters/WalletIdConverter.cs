using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Converters;

internal sealed class WalletIdConverter : StronglyTypedIdConverter<WalletId>
{
    public WalletIdConverter() : base(WalletId.From)
    {
    }
}