namespace Application.Wallet.Features.Commands.FreezeWallet;

public sealed record FreezeWalletCommand(
    Guid UserId,
    string Reason,
    Guid AdminId) : ICommand<Unit>;