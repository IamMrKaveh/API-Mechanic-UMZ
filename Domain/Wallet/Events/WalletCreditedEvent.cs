namespace Domain.Wallet.Events;

public sealed class WalletCreditedEvent : DomainEvent
{
    public int WalletId { get; }
    public int UserId { get; }
    public decimal Amount { get; }
    public WalletReferenceType ReferenceType { get; }
    public int ReferenceId { get; }

    public WalletCreditedEvent(int walletId, int userId, decimal amount, WalletReferenceType referenceType, int referenceId)
    {
        WalletId = walletId;
        UserId = userId;
        Amount = amount;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
    }
}