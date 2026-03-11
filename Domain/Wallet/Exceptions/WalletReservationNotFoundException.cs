using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class WalletReservationNotFoundException(WalletReservationId reservationId) : Exception($"Wallet reservation '{reservationId}' was not found.")
{
}