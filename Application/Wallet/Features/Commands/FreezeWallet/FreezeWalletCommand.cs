namespace Application.Wallet.Features.Commands.FreezeWallet;

public sealed record FreezeWalletCommand(
    Guid UserId,
    string Reason,
    Guid AdminId)
    : ICommand<Unit>, IAuditableCommand
{
    public string AuditEventType => "SecurityEvent";

    public string AuditAction => "WalletFrozen";

    public string? AuditEntityType => "Wallet";

    public string? AuditEntityId => UserId.ToString();
}