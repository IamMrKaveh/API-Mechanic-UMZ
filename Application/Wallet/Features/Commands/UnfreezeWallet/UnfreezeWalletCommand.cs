namespace Application.Wallet.Features.Commands.UnfreezeWallet;

public sealed record UnfreezeWalletCommand(
    Guid UserId,
    Guid AdminId) : ICommand<Unit>;