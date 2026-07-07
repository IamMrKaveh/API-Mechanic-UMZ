namespace Application.Wallet.Features.Commands.MarkWithdrawalPaid;

public sealed record MarkWithdrawalPaidCommand(
    Guid WithdrawalId,
    Guid AdminId,
    string BankReferenceNumber) : ICommand<Unit>;