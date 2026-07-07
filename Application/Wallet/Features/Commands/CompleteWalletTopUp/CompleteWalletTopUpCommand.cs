namespace Application.Wallet.Features.Commands.CompleteWalletTopUp;

public sealed record CompleteWalletTopUpCommand(
    string Authority,
    string Status) : ICommand<CompleteWalletTopUpResult>;

public sealed record CompleteWalletTopUpResult(
    Guid? TopUpId,
    bool IsSucceeded,
    string StatusText,
    string? FailureReason);