using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Exceptions;

public sealed class WalletReservationNotFoundException : DomainException
{
    public WalletReservationId ReservationId { get; }

    public override string ErrorCode => "WALLET_RESERVATION_NOT_FOUND";

    public WalletReservationNotFoundException(WalletReservationId reservationId)
        : base($"رزرو کیف پول با شناسه '{reservationId}' یافت نشد.")
    {
        ReservationId = reservationId;
    }
}