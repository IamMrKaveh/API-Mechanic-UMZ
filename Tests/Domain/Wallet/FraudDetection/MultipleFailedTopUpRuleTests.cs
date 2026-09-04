using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.FraudDetection;
using Domain.Wallet.FraudDetection.Rules;
using Domain.Wallet.ValueObjects;

namespace Tests.Domain.Wallet.FraudDetection;

public class MultipleFailedTopUpRuleTests
{
    private readonly MultipleFailedTopUpRule _sut = new();

    private static FraudEvaluationContext ContextWithFailedCount(int failedCount) =>
        new()
        {
            WalletId = WalletId.NewId(),
            UserId = UserId.NewId(),
            RecentFailedTopUpCount = failedCount,
            EvaluatedAt = DateTime.UtcNow
        };

    [Fact]
    public void RuleName_IsMultipleFailedTopUp()
    {
        _sut.RuleName.ShouldBe(MultipleFailedTopUpRule.Name);
    }

    [Fact]
    public async Task EvaluateAsync_WhenBelowThreshold_DoesNotTrigger()
    {
        var result = await _sut.EvaluateAsync(ContextWithFailedCount(4));

        result.IsTriggered.ShouldBeFalse();
        result.RuleName.ShouldBe(MultipleFailedTopUpRule.Name);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAtThreshold_TriggersWithMediumSeverity()
    {
        var result = await _sut.EvaluateAsync(ContextWithFailedCount(5));

        result.IsTriggered.ShouldBeTrue();
        result.Severity.ShouldBe(FraudAlertSeverity.Medium);
        result.Metadata.ShouldContain("\"failedCount\":5");
        result.Metadata.ShouldContain("\"threshold\":5");
    }

    [Fact]
    public async Task EvaluateAsync_WhenAboveThreshold_Triggers()
    {
        var result = await _sut.EvaluateAsync(ContextWithFailedCount(12));

        result.IsTriggered.ShouldBeTrue();
        result.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task EvaluateAsync_WithZeroFailures_DoesNotTrigger()
    {
        var result = await _sut.EvaluateAsync(ContextWithFailedCount(0));

        result.IsTriggered.ShouldBeFalse();
    }
}
