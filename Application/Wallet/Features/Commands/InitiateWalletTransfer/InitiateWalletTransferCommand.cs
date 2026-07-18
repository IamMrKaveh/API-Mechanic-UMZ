using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Commands.InitiateWalletTransfer;

public sealed record InitiateWalletTransferCommand(
    string RecipientPhoneNumber,
    decimal Amount,
    string? Description)
    : ICommand<InitiateWalletTransferResultDto>, IAuditableCommand
{
    public string AuditEventType => "PaymentEvent";
    public string AuditAction => "TransferInitiated";
    public string? AuditEntityType => "Wallet";
    public string? AuditEntityId => null;
}