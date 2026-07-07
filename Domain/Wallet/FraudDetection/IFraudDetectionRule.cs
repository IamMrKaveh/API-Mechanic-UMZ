namespace Domain.Wallet.FraudDetection;

public interface IFraudDetectionRule
{
    string RuleName { get; }

    Task<FraudRuleResult> EvaluateAsync(FraudEvaluationContext context, CancellationToken ct = default);
}