namespace Application.Wallet.Features.Commands.MarkWithdrawalPaid;

public sealed record MarkWithdrawalPaidCommand(
    Guid WithdrawalId,
    string BankReferenceNumber) : ICommand<Unit>;