using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Localization;

namespace Domain.Wallet.Exceptions;

public sealed class WalletReservationNotFoundException(WalletReservationId reservationId)
    : NotFoundException<WalletReservationId>(
        DomainErrorCodes.Wallet.ReservationNotFound,
        reservationId,
        $"Wallet reservation with id '{reservationId}' was not found.",
        new Dictionary<string, object?> { ["reservationId"] = reservationId.Value })
{
    public WalletReservationId ReservationId => Value;
}
