using Domain.Wallet.Enums;

namespace Domain.Wallet.FraudDetection.Rules;

public sealed class UnusualAmountRule : IFraudDetectionRule
{
    public const string Name = "UnusualAmount";
    private const decimal Multiplier = 10m;
    private const decimal MinimumAverageForEvaluation = 10_000m;

    public string RuleName => Name;

    public Task<FraudRuleResult> EvaluateAsync(FraudEvaluationContext context, CancellationToken ct = default)
    {
        if (context.UserAverageAmount < MinimumAverageForEvaluation)
            return Task.FromResult(FraudRuleResult.NotTriggered(Name));

        var threshold = context.UserAverageAmount * Multiplier;

        var suspicious = context.RecentLedgerEntries
            .Where(e => e.Amount.Amount >= threshold)
            .OrderByDescending(e => e.Amount.Amount)
            .FirstOrDefault();

        if (suspicious is null)
            return Task.FromResult(FraudRuleResult.NotTriggered(Name));

        var severity = suspicious.Amount.Amount >= threshold * 5 ? FraudAlertSeverity.Critical : FraudAlertSeverity.High;
        var description =
            $"مبلغ تراکنش ({suspicious.Amount.Amount:N0}) بیش از {Multiplier} برابر میانگین کاربر ({context.UserAverageAmount:N0}) است.";
        var metadata = $"{{\"amount\":{suspicious.Amount.Amount},\"average\":{context.UserAverageAmount},\"multiplier\":{Multiplier}}}";
        return Task.FromResult(FraudRuleResult.Triggered(Name, severity, description, metadata));
    }
}