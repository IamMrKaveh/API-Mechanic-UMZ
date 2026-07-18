namespace Application.Wallet.Features.Commands.RejectWithdrawal;

public sealed record RejectWithdrawalCommand(
    Guid WithdrawalId,
    string Reason) : ICommand<Unit>;