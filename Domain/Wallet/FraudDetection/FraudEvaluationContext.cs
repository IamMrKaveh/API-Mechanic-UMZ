using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.FraudDetection;

public sealed class FraudEvaluationContext
{
    public WalletId WalletId { get; init; } = default!;
    public UserId UserId { get; init; } = default!;
    public IEnumerable<WalletLedgerEntry> RecentLedgerEntries { get; init; } = [];
    public decimal UserAverageAmount { get; init; }
    public int RecentTopUpCount { get; init; }
    public int RecentFailedTopUpCount { get; init; }
    public int RecentWithdrawalCount { get; init; }
    public DateTime EvaluatedAt { get; init; }
}
