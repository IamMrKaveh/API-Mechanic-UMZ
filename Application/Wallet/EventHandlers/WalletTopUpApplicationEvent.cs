namespace Application.Wallet.EventHandlers;

public record WalletTopUpApplicationEvent(
    Guid UserId,
    Guid TransactionId,
    Guid OrderId) : INotification;