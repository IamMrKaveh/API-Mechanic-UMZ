using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.FraudDetection;
using Domain.Wallet.FraudDetection.Rules;
using Domain.Wallet.ValueObjects;

namespace Tests.Domain.Wallet.FraudDetection;

public class UnusualAmountRuleTests
{
    private readonly UnusualAmountRule _sut = new();

    private static FraudEvaluationContext ContextWith(decimal average, params WalletLedgerEntry[] entries) =>
        new()
        {
            WalletId = WalletId.NewId(),
            UserId = UserId.NewId(),
            UserAverageAmount = average,
            RecentLedgerEntries = entries,
            EvaluatedAt = DateTime.UtcNow
        };

    private static WalletLedgerEntry EntryWithAmount(decimal amount) =>
        new WalletLedgerEntryBuilder().WithAmount(amount).WithBalanceAfter(amount).Build();

    [Fact]
    public void RuleName_IsUnusualAmount()
    {
        _sut.RuleName.ShouldBe(UnusualAmountRule.Name);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAverageBelowMinimum_DoesNotTrigger()
    {
        var context = ContextWith(5_000m, EntryWithAmount(1_000_000m));

        var result = await _sut.EvaluateAsync(context);

        result.IsTriggered.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WhenAllAmountsNormal_DoesNotTrigger()
    {
        var context = ContextWith(
            100_000m,
            EntryWithAmount(50_000m),
            EntryWithAmount(200_000m),
            EntryWithAmount(900_000m));

        var result = await _sut.EvaluateAsync(context);

        result.IsTriggered.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WhenAmountExceedsTenTimesAverage_TriggersWithHighSeverity()
    {
        var context = ContextWith(100_000m, EntryWithAmount(1_500_000m));

        var result = await _sut.EvaluateAsync(context);

        result.IsTriggered.ShouldBeTrue();
        result.Severity.ShouldBe(FraudAlertSeverity.High);
        result.Description.ShouldNotBeNullOrWhiteSpace();
        result.Metadata.ShouldContain("\"multiplier\":10");
    }

    [Fact]
    public async Task EvaluateAsync_WhenAmountExceedsFiftyTimesAverage_TriggersWithCriticalSeverity()
    {
        var context = ContextWith(100_000m, EntryWithAmount(6_000_000m));

        var result = await _sut.EvaluateAsync(context);

        result.IsTriggered.ShouldBeTrue();
        result.Severity.ShouldBe(FraudAlertSeverity.Critical);
    }

    [Fact]
    public async Task EvaluateAsync_WhenNoEntries_DoesNotTrigger()
    {
        var result = await _sut.EvaluateAsync(ContextWith(100_000m));

        result.IsTriggered.ShouldBeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_PicksLargestSuspiciousAmount()
    {
        var context = ContextWith(
            100_000m,
            EntryWithAmount(1_200_000m),
            EntryWithAmount(3_000_000m));

        var result = await _sut.EvaluateAsync(context);

        result.IsTriggered.ShouldBeTrue();
        result.Metadata.ShouldContain("\"amount\":3000000");
    }
}
