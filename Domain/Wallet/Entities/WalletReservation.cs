using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Entities;

public sealed class WalletReservation : Entity<WalletReservationId>
{
    private WalletReservation()
    { }

    public WalletId WalletId { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public string Purpose { get; private set; } = default!;
    public WalletReservationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    internal static WalletReservation Create(WalletReservationId id, WalletId walletId, Money amount, string purpose)
    {
        return new WalletReservation
        {
            Id = id,
            WalletId = walletId,
            Amount = amount,
            Purpose = purpose,
            Status = WalletReservationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    internal void Confirm()
    {
        Status = WalletReservationStatus.Confirmed;
        ResolvedAt = DateTime.UtcNow;
    }

    internal void Release()
    {
        Status = WalletReservationStatus.Released;
        ResolvedAt = DateTime.UtcNow;
    }
}