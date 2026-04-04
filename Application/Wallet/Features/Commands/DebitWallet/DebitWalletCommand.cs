using Application.Common.Results;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;

namespace Application.Wallet.Features.Commands.DebitWallet;

public record DebitWalletCommand(
    UserId UserId,
    decimal Amount,
    WalletTransactionType TransactionType,
    WalletReferenceType ReferenceType,
    int ReferenceId,
    string IdempotencyKey,
    string? CorrelationId = null,
    string? Description = null
) : IRequest<ServiceResult<Unit>>;