using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Commands.InitiateWalletTransfer;

public sealed record InitiateWalletTransferCommand(
    Guid FromUserId,
    string RecipientPhoneNumber,
    decimal Amount,
    string? Description)
    : ICommand<InitiateWalletTransferResultDto>, IAuditableCommand
{
    public string AuditEventType => "PaymentEvent";

    public string AuditAction => "TransferInitiated";

    public string? AuditEntityType => "Wallet";

    public string? AuditEntityId => FromUserId.ToString();
}