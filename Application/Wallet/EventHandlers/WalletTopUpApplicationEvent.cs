namespace Application.Wallet.EventHandlers;

public record WalletTopUpApplicationEvent(int UserId, int TransactionId, int OrderId) : INotification;