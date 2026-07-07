using Domain.Wallet.Enums;

namespace Domain.Wallet.FraudDetection;

public sealed record FraudRuleResult
{
    public bool IsTriggered { get; init; }
    public string RuleName { get; init; } = default!;
    public FraudAlertSeverity Severity { get; init; }
    public string Description { get; init; } = default!;
    public string? Metadata { get; init; }

    public static FraudRuleResult NotTriggered(string ruleName)
        => new() { IsTriggered = false, RuleName = ruleName, Description = string.Empty };

    public static FraudRuleResult Triggered(
        string ruleName,
        FraudAlertSeverity severity,
        string description,
        string? metadata = null)
        => new()
        {
            IsTriggered = true,
            RuleName = ruleName,
            Severity = severity,
            Description = description,
            Metadata = metadata
        };
}