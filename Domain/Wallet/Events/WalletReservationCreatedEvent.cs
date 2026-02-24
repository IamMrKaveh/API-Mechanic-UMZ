namespace Domain.Wallet.Events;

public sealed class WalletReservationCreatedEvent : DomainEvent
{
    public int WalletId { get; }
    public int UserId { get; }
    public decimal Amount { get; }
    public int OrderId { get; }

    public WalletReservationCreatedEvent(int walletId, int userId, decimal amount, int orderId)
    {
        WalletId = walletId;
        UserId = userId;
        Amount = amount;
        OrderId = orderId;
    }
}