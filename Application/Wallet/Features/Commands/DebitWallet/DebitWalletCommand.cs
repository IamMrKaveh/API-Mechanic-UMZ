namespace Application.Wallet.Features.Commands.DebitWallet;

public record DebitWalletCommand(
    int UserId,
    decimal Amount,
    WalletTransactionType TransactionType,
    WalletReferenceType ReferenceType,
    int ReferenceId,
    string IdempotencyKey,
    string? CorrelationId = null,
    string? Description = null
) : IRequest<ServiceResult<Unit>>;