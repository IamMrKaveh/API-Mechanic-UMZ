namespace Application.Wallet.Features.Commands.CancelWalletTransfer;

public sealed record CancelWalletTransferCommand(
    Guid TransferId)
    : ICommand<Unit>;