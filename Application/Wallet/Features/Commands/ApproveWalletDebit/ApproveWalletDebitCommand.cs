namespace Application.Wallet.Features.Commands.ApproveWalletDebit;

public sealed record ApproveWalletDebitCommand(Guid RequestId)
    : ICommand<Unit>, IAuditableCommand
{
    public string AuditEventType => "PaymentEvent";
    public string AuditAction => "WalletDebitApproved";
    public string? AuditEntityType => "WalletDebitRequest";
    public string? AuditEntityId => RequestId.ToString();
}
