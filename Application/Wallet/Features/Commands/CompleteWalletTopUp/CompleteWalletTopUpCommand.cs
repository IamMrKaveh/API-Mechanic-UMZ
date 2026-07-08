namespace Application.Wallet.Features.Commands.CompleteWalletTopUp;

public sealed record CompleteWalletTopUpCommand(
    string Authority,
    string Status)
    : ICommand<CompleteWalletTopUpResult>;