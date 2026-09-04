using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.FraudDetection;
using Domain.Wallet.FraudDetection.Rules;
using Domain.Wallet.ValueObjects;

namespace Tests.Domain.Wallet.FraudDetection;

public class RapidTopUpWithdrawRuleTests
{
    private readonly RapidTopUpWithdrawRule _sut = new();

    private static FraudEvaluationContext ContextWith(int topUps, int withdrawals) =>
        new()
        {
            WalletId = WalletId.NewId(),
            UserId = UserId.NewId(),
            RecentTopUpCount = topUps,
            RecentWithdrawalCount = withdrawals,
            EvaluatedAt = DateTime.UtcNow
        };

    [Fact]
    public void RuleName_IsRapidTopUpWithdraw()
    {
        _sut.RuleName.ShouldBe(RapidTopUpWithdrawRule.Name);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 0)]
    [InlineData(0, 3)]
    [InlineData(2, 0)]
    public async Task EvaluateAsync_WhenPatternIncomplete_DoesNotTrigger(int topUps, int withdrawals)
    {
        var result = await _sut.EvaluateAsync(ContextWith(topUps, withdrawals));

        result.IsTriggered.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WhenRapidTopUpAndWithdraw_TriggersWithCriticalSeverity()
    {
        var result = await _sut.EvaluateAsync(ContextWith(topUps: 2, withdrawals: 1));

        result.IsTriggered.ShouldBeTrue();
        result.Severity.ShouldBe(FraudAlertSeverity.Critical);
        result.RuleName.ShouldBe(RapidTopUpWithdrawRule.Name);
        result.Metadata.ShouldContain("\"topUps\":2");
        result.Metadata.ShouldContain("\"withdrawals\":1");
    }

    [Fact]
    public async Task EvaluateAsync_WhenHighCounts_Triggers()
    {
        var result = await _sut.EvaluateAsync(ContextWith(topUps: 10, withdrawals: 8));

        result.IsTriggered.ShouldBeTrue();
        result.Description.ShouldNotBeNullOrWhiteSpace();
    }
}
