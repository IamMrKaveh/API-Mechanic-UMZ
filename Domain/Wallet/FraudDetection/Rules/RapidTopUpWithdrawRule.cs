using Domain.Wallet.Enums;

namespace Domain.Wallet.FraudDetection.Rules;

public sealed class RapidTopUpWithdrawRule : IFraudDetectionRule
{
    public const string Name = "RapidTopUpWithdraw";
    private const int TopUpThreshold = 2;
    private const int WithdrawalThreshold = 1;

    public string RuleName => Name;

    public Task<FraudRuleResult> EvaluateAsync(FraudEvaluationContext context, CancellationToken ct = default)
    {
        if (context.RecentTopUpCount >= TopUpThreshold && context.RecentWithdrawalCount >= WithdrawalThreshold)
        {
            var description =
                $"الگوی شارژ و برداشت سریع شناسایی شد: {context.RecentTopUpCount} شارژ و {context.RecentWithdrawalCount} برداشت در بازه اخیر.";
            var metadata = $"{{\"topUps\":{context.RecentTopUpCount},\"withdrawals\":{context.RecentWithdrawalCount}}}";
            return Task.FromResult(FraudRuleResult.Triggered(Name, FraudAlertSeverity.Critical, description, metadata));
        }

        return Task.FromResult(FraudRuleResult.NotTriggered(Name));
    }
}