using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.FraudDetection;
using Domain.Wallet.ValueObjects;

namespace Tests.Domain.Wallet.FraudDetection;

public class FraudEvaluationContextTests
{
    [Fact]
    public void DefaultEntries_AreEmpty()
    {
        var sut = new FraudEvaluationContext
        {
            WalletId = WalletId.NewId(),
            UserId = UserId.NewId(),
            EvaluatedAt = DateTime.UtcNow
        };

        sut.RecentLedgerEntries.ShouldBeEmpty();
        sut.UserAverageAmount.ShouldBe(0m);
        sut.RecentTopUpCount.ShouldBe(0);
        sut.RecentFailedTopUpCount.ShouldBe(0);
        sut.RecentWithdrawalCount.ShouldBe(0);
    }

    [Fact]
    public void InitProperties_RetainAssignedValues()
    {
        var walletId = WalletId.NewId();
        var userId = UserId.NewId();
        var entries = new[] { new WalletLedgerEntryBuilder().Build() };
        var evaluatedAt = DateTime.UtcNow;

        var sut = new FraudEvaluationContext
        {
            WalletId = walletId,
            UserId = userId,
            RecentLedgerEntries = entries,
            UserAverageAmount = 250_000m,
            RecentTopUpCount = 3,
            RecentFailedTopUpCount = 1,
            RecentWithdrawalCount = 2,
            EvaluatedAt = evaluatedAt
        };

        sut.WalletId.ShouldBe(walletId);
        sut.UserId.ShouldBe(userId);
        sut.RecentLedgerEntries.ShouldBe(entries);
        sut.UserAverageAmount.ShouldBe(250_000m);
        sut.RecentTopUpCount.ShouldBe(3);
        sut.RecentFailedTopUpCount.ShouldBe(1);
        sut.RecentWithdrawalCount.ShouldBe(2);
        sut.EvaluatedAt.ShouldBe(evaluatedAt);
    }
}
