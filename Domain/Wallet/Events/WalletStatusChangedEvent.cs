namespace Domain.Wallet.Events;

public sealed class WalletStatusChangedEvent(int walletId, int userId, WalletStatus newStatus, string? reason) : DomainEvent
{
    public int WalletId { get; } = walletId;
    public int UserId { get; } = userId;
    public WalletStatus NewStatus { get; } = newStatus;
    public string? Reason { get; } = reason;
}