using Domain.Wallet.Enums;

namespace Application.Wallet.Features.Commands.DebitWallet;

public record DebitWalletCommand(
    Guid UserId,
    decimal Amount,
    WalletTransactionType TransactionType,
    WalletReferenceType ReferenceType,
    Guid ReferenceId,
    string IdempotencyKey,
    Guid? CorrelationId = null,
    string? Description = null) : IRequest<ServiceResult<Unit>>;