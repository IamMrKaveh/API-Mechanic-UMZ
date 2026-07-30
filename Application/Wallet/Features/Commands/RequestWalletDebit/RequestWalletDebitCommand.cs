namespace Application.Wallet.Features.Commands.RequestWalletDebit;

public sealed record RequestWalletDebitCommand(
    Guid UserId,
    decimal Amount,
    string Reason,
    string? Description,
    string IdempotencyKey,
    int ExpiryHours = 72)
    : ICommand<Guid>, IAuditableCommand
{
    public string AuditEventType => "PaymentEvent";
    public string AuditAction => "WalletDebitRequested";
    public string? AuditEntityType => "Wallet";
    public string? AuditEntityId => UserId.ToString();
}
