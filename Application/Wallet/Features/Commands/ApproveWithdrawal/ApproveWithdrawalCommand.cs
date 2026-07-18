namespace Application.Wallet.Features.Commands.ApproveWithdrawal;

public sealed record ApproveWithdrawalCommand(
    Guid WithdrawalId) : ICommand<Unit>;