using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Converters;

internal sealed class WalletFraudAlertIdConverter : StronglyTypedIdConverter<WalletFraudAlertId>
{
    public WalletFraudAlertIdConverter() : base(WalletFraudAlertId.From)
    {
    }
}