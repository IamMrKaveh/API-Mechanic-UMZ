using Application.Common.Interfaces;
using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Commands.ConfirmWalletTransfer;

public sealed record ConfirmWalletTransferCommand(
    Guid TransferId,
    Guid FromUserId,
    string OtpCode) : ICommand<ConfirmWalletTransferResultDto>;