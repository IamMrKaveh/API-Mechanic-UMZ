namespace Application.Wallet.Features.Commands.RejectWithdrawal;

public sealed record RejectWithdrawalCommand(
    Guid WithdrawalId,
    Guid AdminId,
    string Reason) : ICommand<Unit>;