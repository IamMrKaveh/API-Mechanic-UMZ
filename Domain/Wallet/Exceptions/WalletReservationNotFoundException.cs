using Domain.Wallet.ValueObjects;
using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class WalletReservationNotFoundException : DomainException
{
    public WalletReservationNotFoundException(WalletReservationId reservationId)
        : base(
            DomainErrorCodes.Wallet.ReservationNotFound,
            $"Wallet reservation with id '{reservationId}' was not found.",
            new Dictionary<string, object?> { ["reservationId"] = reservationId.Value })
    {
    }
}
