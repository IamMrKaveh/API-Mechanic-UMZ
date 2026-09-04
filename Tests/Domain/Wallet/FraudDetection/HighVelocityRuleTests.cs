using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.FraudDetection;
using Domain.Wallet.FraudDetection.Rules;
using Domain.Wallet.ValueObjects;

namespace Tests.Domain.Wallet.FraudDetection;

public class HighVelocityRuleTests
{
    private readonly HighVelocityRule _sut = new();

    private static FraudEvaluationContext ContextWithEntries(params WalletLedgerEntry[] entries) =>
        new()
        {
            WalletId = WalletId.NewId(),
            UserId = UserId.NewId(),
            RecentLedgerEntries = entries,
            EvaluatedAt = DateTime.UtcNow
        };

    private static WalletLedgerEntry NewEntry() =>
        new WalletLedgerEntryBuilder().Build();

    [Fact]
    public void RuleName_IsHighVelocity()
    {
        _sut.RuleName.ShouldBe(HighVelocityRule.Name);
        _sut.RuleName.ShouldBe("HighVelocity");
    }

    [Fact]
    public async Task EvaluateAsync_WhenFewerThanTenRecentTransactions_DoesNotTrigger()
    {
        var context = ContextWithEntries(Enumerable.Range(0, 9).Select(_ => NewEntry()).ToArray());

        var result = await _sut.EvaluateAsync(context);

        result.IsTriggered.ShouldBeFalse();
        result.RuleName.ShouldBe(HighVelocityRule.Name);
    }

    [Fact]
    public async Task EvaluateAsync_WhenTenRecentTransactions_TriggersWithHighSeverity()
    {
        var context = ContextWithEntries(Enumerable.Range(0, 10).Select(_ => NewEntry()).ToArray());

        var result = await _sut.EvaluateAsync(context);

        result.IsTriggered.ShouldBeTrue();
        result.Severity.ShouldBe(FraudAlertSeverity.High);
        result.Description.ShouldNotBeNullOrWhiteSpace();
        result.Metadata.ShouldContain("\"threshold\":10");
    }

    [Fact]
    public async Task EvaluateAsync_WhenMoreThanThreshold_Triggers()
    {
        var context = ContextWithEntries(Enumerable.Range(0, 25).Select(_ => NewEntry()).ToArray());

        var result = await _sut.EvaluateAsync(context);

        result.IsTriggered.ShouldBeTrue();
        result.Metadata.ShouldContain("\"count\":25");
    }

    [Fact]
    public async Task EvaluateAsync_WithNoEntries_DoesNotTrigger()
    {
        var result = await _sut.EvaluateAsync(ContextWithEntries());

        result.IsTriggered.ShouldBeFalse();
    }
}
