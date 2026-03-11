namespace Domain.Wallet.Projections;

public sealed record ExpiredReservationProjection(
    int ReservationId,
    int WalletId,
    decimal Amount,
    int OrderId,
    DateTime ExpiresAt);