namespace Application.Wallet.EventHandlers;

public record WalletRefundApplicationEvent(int TransactionId, int OrderId, decimal Amount) : INotification;