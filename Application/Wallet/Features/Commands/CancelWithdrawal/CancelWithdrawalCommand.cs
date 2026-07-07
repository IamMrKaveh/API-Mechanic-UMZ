namespace Application.Wallet.Features.Commands.CancelWithdrawal;

public sealed record CancelWithdrawalCommand(
    Guid WithdrawalId,
    Guid UserId) : ICommand<Unit>;