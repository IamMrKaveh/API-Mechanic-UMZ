namespace Application.Wallet.Features.Commands.CancelWithdrawal;

public sealed record CancelWithdrawalCommand(
    Guid WithdrawalId) : ICommand<Unit>;