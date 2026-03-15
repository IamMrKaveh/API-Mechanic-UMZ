using Domain.Wallet.Entities;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Results;

public sealed record WalletReservationResult
{
    public bool IsSuccess { get; private init; }
    public WalletId? WalletId { get; private init; }
    public WalletReservation? Reservation { get; private init; }
    public string? Error { get; private init; }

    private WalletReservationResult()
    {
    }

    public static WalletReservationResult Success(WalletId walletId, WalletReservation reservation) =>
        new() { IsSuccess = true, WalletId = walletId, Reservation = reservation };

    public static WalletReservationResult InsufficientBalance(WalletId walletId, Money requested, Money available) =>
        new()
        {
            IsSuccess = false,
            WalletId = walletId,
            Error = $"موجودی کافی نیست. درخواستی: {requested.ToTomanString()}، موجود: {available.ToTomanString()}"
        };

    public static WalletReservationResult Failed(string error) =>
        new() { IsSuccess = false, Error = error };
}