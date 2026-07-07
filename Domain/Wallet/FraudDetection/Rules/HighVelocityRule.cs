using Domain.Wallet.Enums;

namespace Domain.Wallet.FraudDetection.Rules;

public sealed class HighVelocityRule : IFraudDetectionRule
{
    public const string Name = "HighVelocity";
    private const int TransactionThreshold = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    public string RuleName => Name;

    public Task<FraudRuleResult> EvaluateAsync(FraudEvaluationContext context, CancellationToken ct = default)
    {
        var cutoff = context.EvaluatedAt.Subtract(Window);
        var count = context.RecentLedgerEntries.Count(e => e.OccurredAt >= cutoff);

        if (count >= TransactionThreshold)
        {
            var description =
                $"{count} تراکنش در {Window.TotalMinutes:F0} دقیقه اخیر ثبت شده است (آستانه: {TransactionThreshold}).";
            var metadata = $"{{\"count\":{count},\"windowMinutes\":{Window.TotalMinutes:F0},\"threshold\":{TransactionThreshold}}}";
            return Task.FromResult(FraudRuleResult.Triggered(Name, FraudAlertSeverity.High, description, metadata));
        }

        return Task.FromResult(FraudRuleResult.NotTriggered(Name));
    }
}