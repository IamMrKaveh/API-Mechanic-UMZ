namespace Application.Wallet.Features.Commands.ForceFreezeFromFraudAlert;

public sealed record ForceFreezeFromFraudAlertCommand(
    Guid AlertId,
    string? AdditionalNote)
    : ICommand<Unit>, IAuditableCommand
{
    public string AuditEventType => "SecurityEvent";
    public string AuditAction => "WalletForceFrozenFromFraudAlert";
    public string? AuditEntityType => "WalletFraudAlert";
    public string? AuditEntityId => AlertId.ToString();
}
