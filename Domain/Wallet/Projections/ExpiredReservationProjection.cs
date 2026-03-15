using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Projections;

public sealed record ExpiredReservationProjection(
    WalletReservationId ReservationId,
    WalletId WalletId,
    Money Amount,
    string Purpose,
    DateTime ExpiresAt);