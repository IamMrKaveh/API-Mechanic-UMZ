using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Commands.ConfirmWalletTransfer;

public sealed record ConfirmWalletTransferCommand(
    Guid TransferId,
    string OtpCode) : ICommand<ConfirmWalletTransferResultDto>;