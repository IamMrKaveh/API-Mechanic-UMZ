namespace Domain.Wallet.Events;

public sealed class WalletStatusChangedEvent : DomainEvent
{
    public int WalletId { get; }
    public int UserId { get; }
    public WalletStatus NewStatus { get; }
    public string? Reason { get; }

    public WalletStatusChangedEvent(int walletId, int userId, WalletStatus newStatus, string? reason)
    {
        WalletId = walletId;
        UserId = userId;
        NewStatus = newStatus;
        Reason = reason;
    }
}