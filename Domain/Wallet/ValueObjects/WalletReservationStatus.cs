namespace Domain.Wallet.ValueObjects;

public enum WalletReservationStatus
{
    Pending,
    Committed,
    Released,
    Expired
}