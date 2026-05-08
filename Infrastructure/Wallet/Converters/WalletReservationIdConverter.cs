using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Converters;

internal sealed class WalletReservationIdConverter : StronglyTypedIdConverter<WalletReservationId>
{
    public WalletReservationIdConverter() : base(WalletReservationId.From)
    {
    }
}