namespace Domain.Wallet.Exceptions;

public sealed class WalletReservationNotFoundException(WalletReservationId reservationId) : DomainException($"رزرو کیف پول با شناسه '{reservationId}' یافت نشد.")
{
    public WalletReservationId ReservationId { get; } = reservationId;
}