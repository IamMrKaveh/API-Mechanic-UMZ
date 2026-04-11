namespace Application.Wallet.EventHandlers;

public record WalletRefundApplicationEvent(
    Guid TransactionId,
    Guid OrderId,
    decimal Amount) : INotification;