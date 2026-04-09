using Domain.Wallet.Enums;

namespace Application.Wallet.Features.Commands.CreditWallet;

public record CreditWalletCommand(
    Guid UserId,
    decimal Amount,
    WalletTransactionType TransactionType,
    WalletReferenceType ReferenceType,
    int ReferenceId,
    string IdempotencyKey,
    string? CorrelationId = null,
    string? Description = null) : IRequest<ServiceResult<Unit>>;