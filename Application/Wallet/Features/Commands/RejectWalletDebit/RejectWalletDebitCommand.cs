namespace Application.Wallet.Features.Commands.RejectWalletDebit;

public sealed record RejectWalletDebitCommand(Guid RequestId, string? RejectionReason) : ICommand<Unit>;
