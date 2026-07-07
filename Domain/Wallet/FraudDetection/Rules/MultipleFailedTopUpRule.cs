using Domain.Wallet.Enums;

namespace Domain.Wallet.FraudDetection.Rules;

public sealed class MultipleFailedTopUpRule : IFraudDetectionRule
{
    public const string Name = "MultipleFailedTopUp";
    private const int FailedThreshold = 5;

    public string RuleName => Name;

    public Task<FraudRuleResult> EvaluateAsync(FraudEvaluationContext context, CancellationToken ct = default)
    {
        if (context.RecentFailedTopUpCount >= FailedThreshold)
        {
            var description =
                $"{context.RecentFailedTopUpCount} تلاش ناموفق شارژ در بازه اخیر ثبت شده است (آستانه: {FailedThreshold}).";
            var metadata = $"{{\"failedCount\":{context.RecentFailedTopUpCount},\"threshold\":{FailedThreshold}}}";
            return Task.FromResult(FraudRuleResult.Triggered(Name, FraudAlertSeverity.Medium, description, metadata));
        }

        return Task.FromResult(FraudRuleResult.NotTriggered(Name));
    }
}