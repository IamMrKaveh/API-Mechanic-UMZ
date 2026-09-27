using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletDebitedEvent(
    WalletId WalletId,
    UserId UserId,
    Money Amount,
    Money NewBalance,
    string Description,
    string ReferenceId,
    string? IdempotencyKey = null,
    string? CorrelationId = null,
    WalletDebitRequestId? DebitRequestId = null,
    WalletWithdrawalRequestId? WithdrawalRequestId = null,
    WalletTransferId? TransferId = null,
    WalletTopUpId? TopUpId = null) : DomainEvent
{
    public UserId OwnerId => UserId;
    public new string? CorrelationId { get; } = CorrelationId;
}
