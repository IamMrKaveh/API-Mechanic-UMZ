namespace Domain.Wallet.Events;

public sealed class WalletReservationCommittedEvent(int walletId, int userId, decimal amount, int orderId) : DomainEvent
{
    public int WalletId { get; } = walletId;
    public int UserId { get; } = userId;
    public decimal Amount { get; } = amount;
    public int OrderId { get; } = orderId;
}